using GestionFinanciera.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFinanciera.Infrastructure.Persistence.Configurations;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.ResolutionNote)
            .HasMaxLength(500);

        builder.Property(c => c.ProposedAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(c => new { c.CompanyId, c.Status });

        builder.HasOne(c => c.Company)
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Movement)
            .WithMany()
            .HasForeignKey(c => c.MovementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ClaimantAccount)
            .WithMany()
            .HasForeignKey(c => c.ClaimantAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
