using GestionFinanciera.Application.Abstractions;
using GestionFinanciera.Application.Common.Audit;
using GestionFinanciera.Application.Features.Auth.Interfaces;
using GestionFinanciera.Application.Features.Categories;
using GestionFinanciera.Application.Features.Categories.Interfaces;
using GestionFinanciera.Application.Features.Companies.Interfaces;
using GestionFinanciera.Application.Features.Dashboard;
using GestionFinanciera.Application.Features.Dashboard.Interfaces;
using GestionFinanciera.Application.Features.Reports;
using GestionFinanciera.Application.Features.Reports.Interfaces;
using GestionFinanciera.Application.Features.Transactions;
using GestionFinanciera.Application.Features.Transactions.Interfaces;
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

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<IExcelService, ExcelService>();
        services.AddScoped<IEmailService, EmailService>();

        // Repositories
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();

        return services;
    }
}
