using GestionFinanciera.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFinanciera.Infrastructure.Persistence.Configurations;

public sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(l => l.RepaidAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(l => l.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(l => l.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.DecisionNote)
            .HasMaxLength(500);

        builder.HasIndex(l => new { l.CompanyId, l.Status });

        builder.HasOne(l => l.Company)
            .WithMany()
            .HasForeignKey(l => l.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ClientAccount)
            .WithMany()
            .HasForeignKey(l => l.ClientAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
