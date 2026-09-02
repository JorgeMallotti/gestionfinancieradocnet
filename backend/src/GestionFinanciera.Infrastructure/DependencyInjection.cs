using GestionFinanciera.Application.Abstractions;
using GestionFinanciera.Application.Common.Audit;
using GestionFinanciera.Application.Features.Accounts;
using GestionFinanciera.Application.Features.Accounts.Interfaces;
using GestionFinanciera.Application.Features.Auth.Interfaces;
using GestionFinanciera.Application.Features.Categories;
using GestionFinanciera.Application.Features.Categories.Interfaces;
using GestionFinanciera.Application.Features.Claims;
using GestionFinanciera.Application.Features.Claims.Interfaces;
using GestionFinanciera.Application.Features.Companies.Interfaces;
using GestionFinanciera.Application.Features.Dashboard;
using GestionFinanciera.Application.Features.Dashboard.Interfaces;
using GestionFinanciera.Application.Features.Loans;
using GestionFinanciera.Application.Features.Loans.Interfaces;
using GestionFinanciera.Application.Features.Movements;
using GestionFinanciera.Application.Features.Movements.Interfaces;
using GestionFinanciera.Application.Features.Notifications;
using GestionFinanciera.Application.Features.Notifications.Interfaces;
using GestionFinanciera.Application.Features.Reports;
using GestionFinanciera.Application.Features.Reports.Interfaces;
using GestionFinanciera.Infrastructure.Identity;
using GestionFinanciera.Infrastructure.Persistence;
using GestionFinanciera.Infrastructure.Persistence.Repositories;
using GestionFinanciera.Infrastructure.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionFinanciera.Infrastructure;

/// <summary>Registers all Infrastructure services (EF Core, Identity, repositories).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Identity with ApplicationUser and Guid keys
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;

                // Account lockout — 5 failed attempts → 15 min lock
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // Ambient tenant/user resolution
        services.AddSingleton<ICurrentTenant, CurrentTenantService>();
        services.AddSingleton<ICurrentUser, CurrentUserService>();

        // JWT
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<JwtService>();

        // SMTP (reports by email) — real values via user-secrets / App Settings
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        // Demo quick access (MVP demo) — enabled via Demo:Enabled config
        services.Configure<DemoOptions>(configuration.GetSection(DemoOptions.SectionName));
        services.AddScoped<DemoSeeder>();

        // Periodic demo data reset (BackgroundService + PeriodicTimer, no endpoint)
        services.AddHostedService<DemoResetService>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IMovementService, MovementService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IClaimService, ClaimService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<IExcelService, ExcelService>();
        services.AddScoped<IEmailService, EmailService>();

        // Repositories
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IMovementRepository, MovementRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();

        return services;
    }
}
