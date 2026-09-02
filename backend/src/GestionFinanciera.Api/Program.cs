using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

using FluentValidation;

using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Api.Middleware;
using GestionFinanciera.Application.Features.Auth.DTOs;
using GestionFinanciera.Infrastructure;
using GestionFinanciera.Infrastructure.Identity;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

// ── Serilog bootstrap logger (used if startup fails before config loads) ──
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/gestionfinanciera-.log", rollingInterval: RollingInterval.Day));

    // ── Controllers + strict JSON (unknown fields reject the request) ──
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        });

    // RFC 7807 ProblemDetails for errors (no stack traces leaked to clients)
    builder.Services.AddProblemDetails();

    // Global exception handler → ProblemDetails (see Middleware/GlobalExceptionHandler.cs)
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // Forwarded headers: the real client IP/scheme arrive via X-Forwarded-* when
    // behind a reverse proxy (Azure App Service). Enabled via configuration in prod.
    bool forwardedHeadersEnabled = builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");
    if (forwardedHeadersEnabled)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // App Service pattern (Microsoft docs): the only entry point is the
            // Azure load balancer, so trust the forwarded headers it sets.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    // ── Swagger / OpenAPI ──
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "GestionFinanciera API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter the JWT access token.",
        });
        c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer", doc, "securityScheme"),
                new List<string>()
            },
        });
    });

    // ── FluentValidation (one validator per write DTO) ──
    builder.Services.AddValidatorsFromAssembly(typeof(RegisterDto).Assembly);

    // ── Infrastructure: EF Core, Identity, JWT, repositories ──
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── JWT Bearer authentication ──
    IConfigurationSection jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSection["SigningKey"] ?? string.Empty)),
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });

    builder.Services.AddAuthorization();

    // ── CORS: explicit origins only, never wildcard ──
    string[] allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? ["http://localhost:4200"];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("frontend", policy =>
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()); // required for the refresh cookie
    });

    // ── Rate limiting ────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // "auth": public endpoints (login/register/demo-login/refresh).
        // Keyed by IP — anonymous callers have no identity to partition on.
        options.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

        // "reports": authenticated but CPU/IO-heavy operations (PDF/Excel
        // generation, report-by-email). Partitioned by the USER (their JWT),
        // not the IP — a shared NAT/office IP must not lock a whole team out,
        // and a single abuser cannot be hidden behind rotating IPs.
        options.AddPolicy("reports", httpContext =>
        {
            Guid userId = httpContext.User.GetUserId();
            string partitionKey = userId != Guid.Empty
                ? $"user:{userId}"
                : $"ip:{httpContext.Connection.RemoteIpAddress}";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
        });
    });

    // ── Request body size limit (1 MB) ──
    builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(
        o => o.Limits.MaxRequestBodySize = 1_048_576);

    // ── Health checks (App Service / load balancers) ──
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Must run before UseHttpsRedirection/UseAuthentication so scheme and client
    // IP are correct behind a reverse proxy (no-op when ForwardedHeaders is off).
    app.UseForwardedHeaders();

    app.UseExceptionHandler(); // global handler registered above

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<SecurityHeadersMiddleware>();

    app.UseHttpsRedirection();

    app.UseRateLimiter();
    app.UseCors("frontend");

    app.UseAuthentication();
    app.UseMiddleware<TenantMiddleware>(); // resolves tenant/user from the JWT
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    // Demo quick access seeding — no-op when Demo:Enabled=false. Runs once at
    // startup with NO tenant (request context), so the multi-tenant query
    // filters are neutral. Idempotent: existing accounts/data are left intact.
    using (IServiceScope scope = app.Services.CreateScope())
    {
        var demoSeeder = scope.ServiceProvider.GetRequiredService<DemoSeeder>();
        await demoSeeder.SeedAsync(CancellationToken.None);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
