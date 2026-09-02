using GestionFinanciera.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFinanciera.Infrastructure.Persistence.Configurations;

public sealed class MovementConfiguration : IEntityTypeConfiguration<Movement>
{
    public void Configure(EntityTypeBuilder<Movement> builder)
    {
        builder.ToTable("Movements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(m => m.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(500);

        // The ledger is queried per account and by date (dashboard, reports).
        builder.HasIndex(m => new { m.CompanyId, m.FromAccountId, m.OccurredAt });
        builder.HasIndex(m => new { m.CompanyId, m.ToAccountId, m.OccurredAt });

        builder.HasOne(m => m.Company)
            .WithMany(c => c.Movements)
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.FromAccount)
            .WithMany(a => a.OutgoingMovements)
            .HasForeignKey(m => m.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ToAccount)
            .WithMany(a => a.IncomingMovements)
            .HasForeignKey(m => m.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Corrective transfers link back to the original movement (append-only).
        builder.HasOne(m => m.CorrectsMovement)
            .WithMany()
            .HasForeignKey(m => m.CorrectsMovementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
