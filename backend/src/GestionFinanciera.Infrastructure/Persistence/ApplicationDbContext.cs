using GestionFinanciera.Application.Abstractions;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionFinanciera.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext. Uses Identity tables for users/roles plus the business
/// entities. Global query filters enforce tenant isolation automatically.
/// </summary>
public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentTenant currentTenant)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ── Multi-tenancy: global query filters ─────────────────────────────
        // Automatic isolation — a query can never leak another company's rows.
        //
        // IMPORTANT: the filter MUST reference the tenant provider instance
        // (evaluated per DbContext, i.e. per request), NOT a captured local
        // variable. EF Core caches the model once per process — capturing a
        // local would freeze the FIRST tenant forever.
        builder.Entity<Category>()
            .HasQueryFilter(c => _currentTenant.CompanyId == null || c.CompanyId == _currentTenant.CompanyId);

        builder.Entity<Transaction>()
            .HasQueryFilter(t => _currentTenant.CompanyId == null || t.CompanyId == _currentTenant.CompanyId);

        builder.Entity<AuditLog>()
            .HasQueryFilter(a => _currentTenant.CompanyId == null || a.CompanyId == _currentTenant.CompanyId);
    }
}
