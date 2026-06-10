using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data.Configurations;

public sealed class ProcurementConfiguration : IEntityTypeConfiguration<Procurement>
{
    public void Configure(EntityTypeBuilder<Procurement> entity)
    {
        entity.HasKey(procurement => procurement.ProcurementId);
        entity.Property(procurement => procurement.ProcurementId).ValueGeneratedNever();
        entity.Property(procurement => procurement.ProcNum).IsRequired();
        entity.Property(procurement => procurement.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        entity.Property(procurement => procurement.ContractType).HasConversion<string>().HasMaxLength(50);
        entity
            .Property(procurement => procurement.ProjectRegion)
            .HasConversion(
                region => region.ToString(),
                value => value == "SMRT" ? ProjectRegion.SMTR : Enum.Parse<ProjectRegion>(value)
            )
            .HasMaxLength(50);
        entity
            .Property(procurement => procurement.ProcurementCategory)
            .HasConversion<string>()
            .HasMaxLength(50);
        entity
            .Property(procurement => procurement.ProcurementStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity
            .HasIndex(procurement => procurement.ProcNum)
            .IsUnique()
            .HasDatabaseName("AK_Procurements_ProcNum")
            .HasFilter("[IsDeleted] = 0 AND [ProcNum] IS NOT NULL");
        entity
            .HasIndex(procurement => new { procurement.UserId, procurement.CreatedAt })
            .HasDatabaseName("IX_Procurements_UserId_CreatedAt")
            .IsDescending(false, true);

        entity
            .HasOne(procurement => procurement.Status)
            .WithMany()
            .HasForeignKey(procurement => procurement.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne(procurement => procurement.User)
            .WithMany(user => user.Procurements)
            .HasForeignKey(procurement => procurement.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne(procurement => procurement.JobType)
            .WithMany(job => job.Procurements)
            .HasForeignKey(procurement => procurement.JobTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        ConfigureChildren(entity);
        ConfigureTrackingUsers(entity);
    }

    private static void ConfigureChildren(EntityTypeBuilder<Procurement> entity)
    {
        entity.HasMany(p => p.ProcDetails).WithOne(d => d.Procurement).HasForeignKey(d => d.ProcurementId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(p => p.ProcOffers).WithOne(o => o.Procurement).HasForeignKey(o => o.ProcurementId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(p => p.ProcDocuments).WithOne(d => d.Procurement).HasForeignKey(d => d.ProcurementId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(p => p.VendorOffers).WithOne(vo => vo.Procurement).HasForeignKey(vo => vo.ProcurementId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(p => p.ProfitLosses).WithOne(pl => pl.Procurement).HasForeignKey(pl => pl.ProcurementId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(p => p.StatusHistories).WithOne(h => h.Procurement).HasForeignKey(h => h.ProcurementId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureTrackingUsers(EntityTypeBuilder<Procurement> entity)
    {
        entity.HasOne(p => p.AccrualFilledByUser).WithMany().HasForeignKey(p => p.AccrualFilledByUserId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(p => p.ApInvoiceUser).WithMany().HasForeignKey(p => p.ApInvoiceUserId).OnDelete(DeleteBehavior.NoAction);
        entity.HasOne(p => p.ArUser).WithMany().HasForeignKey(p => p.ArUserId).OnDelete(DeleteBehavior.NoAction);
        entity.HasOne(p => p.IspaSubmittedByUser).WithMany().HasForeignKey(p => p.IspaSubmittedByUserId).OnDelete(DeleteBehavior.NoAction);
        entity.HasOne(p => p.PoSubmittedByUser).WithMany().HasForeignKey(p => p.PoSubmittedByUserId).OnDelete(DeleteBehavior.NoAction);
        entity.HasOne(p => p.HardcopySubmittedByUser).WithMany().HasForeignKey(p => p.HardcopySubmittedByUserId).OnDelete(DeleteBehavior.NoAction);
        entity.HasOne(p => p.RejectedByUser).WithMany().HasForeignKey(p => p.RejectedByUserId).OnDelete(DeleteBehavior.NoAction);
        entity.HasOne(p => p.ApprovalSentByUser).WithMany().HasForeignKey(p => p.ApprovalSentByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ProcOfferConfiguration : IEntityTypeConfiguration<ProcOffer>
{
    public void Configure(EntityTypeBuilder<ProcOffer> entity)
    {
        entity.Property(procOffer => procOffer.ProcOfferId).ValueGeneratedNever();
    }
}
