using GestionFinanciera.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFinanciera.Infrastructure.Persistence.Configurations;

public sealed class ClientAccountConfiguration : IEntityTypeConfiguration<ClientAccount>
{
    public void Configure(EntityTypeBuilder<ClientAccount> builder)
    {
        builder.ToTable("ClientAccounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.DisplayName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(a => a.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.Currency)
            .HasMaxLength(3)
            .IsRequired();

        // One user owns at most one account per bank.
        builder.HasIndex(a => new { a.CompanyId, a.OwnerUserId })
            .IsUnique();

        builder.HasIndex(a => new { a.CompanyId, a.Status });

        builder.HasOne(a => a.Company)
            .WithMany(c => c.ClientAccounts)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // The owner ApplicationUser is an Identity row — no FK navigation here
        // (Identity owns its own store). OwnerUserId stays a plain Guid column.
    }
}
