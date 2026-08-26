using GestionFinanciera.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFinanciera.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.Date)
            .IsRequired();

        // Dashboard queries filter by (CompanyId, Date) — index both.
        builder.HasIndex(t => new { t.CompanyId, t.Date });

        builder.HasOne(t => t.Company)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
