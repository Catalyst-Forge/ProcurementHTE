using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data.Configurations;

public sealed class PurchaseRequisitionConfiguration : IEntityTypeConfiguration<PurchaseRequisition>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisition> entity)
    {
        entity.HasKey(pr => pr.PrId);

        entity.Property(pr => pr.PrId).ValueGeneratedNever();
        entity.Property(pr => pr.PrNumber).IsRequired().HasMaxLength(100);
        entity.Property(pr => pr.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        entity
            .HasIndex(pr => pr.PrNumber)
            .IsUnique()
            .HasDatabaseName("AK_PurchaseRequisitions_PrNumber")
            .HasFilter("[IsDeleted] = 0");

        entity
            .HasOne(pr => pr.CreatedByUser)
            .WithMany()
            .HasForeignKey(pr => pr.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasMany(pr => pr.Procurements)
            .WithOne(p => p.PurchaseRequisition)
            .HasForeignKey(p => p.PrId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
