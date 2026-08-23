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
        // When no tenant is set (anonymous endpoints), the filter is a no-op
        // because those endpoints never query business tables.
        Guid? tenantId = currentTenant.CompanyId;

        builder.Entity<Category>()
            .HasQueryFilter(c => tenantId == null || c.CompanyId == tenantId);

        builder.Entity<Transaction>()
            .HasQueryFilter(t => tenantId == null || t.CompanyId == tenantId);

        builder.Entity<AuditLog>()
            .HasQueryFilter(a => tenantId == null || a.CompanyId == tenantId);
    }
}
