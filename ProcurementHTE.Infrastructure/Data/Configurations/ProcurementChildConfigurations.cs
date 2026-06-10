using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data.Configurations;

public sealed class ProcurementStatusHistoryConfiguration : IEntityTypeConfiguration<ProcurementStatusHistory>
{
    public void Configure(EntityTypeBuilder<ProcurementStatusHistory> entity)
    {
        entity.HasKey(history => history.Id);
        entity.Property(history => history.Id).ValueGeneratedNever();
        entity.Property(history => history.ChangedAt).HasDefaultValueSql("GETUTCDATE()");
        entity.Property(history => history.Status).HasConversion<string>().HasMaxLength(50);

        entity.HasIndex(history => history.ProcurementId);
        entity.HasIndex(history => new { history.ProcurementId, history.ChangedAt });

        entity
            .HasOne(history => history.Procurement)
            .WithMany(procurement => procurement.StatusHistories)
            .HasForeignKey(history => history.ProcurementId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(history => history.ChangedByUser)
            .WithMany()
            .HasForeignKey(history => history.ChangedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class JobTypesConfiguration : IEntityTypeConfiguration<JobTypes>
{
    public void Configure(EntityTypeBuilder<JobTypes> entity)
    {
        entity.Property(job => job.JobTypeId).ValueGeneratedNever();
        entity.HasMany(job => job.Procurements).WithOne(p => p.JobType).HasForeignKey(p => p.JobTypeId).OnDelete(DeleteBehavior.NoAction);
        entity.HasMany(job => job.JobTypeDocuments).WithOne(jtd => jtd.JobType).HasForeignKey(jtd => jtd.JobTypeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProcDetailConfiguration : IEntityTypeConfiguration<ProcDetail>
{
    public void Configure(EntityTypeBuilder<ProcDetail> entity)
    {
        entity.Property(detail => detail.ProcDetailId).ValueGeneratedNever();

        entity
            .HasOne(detail => detail.Procurement)
            .WithMany(procurement => procurement.ProcDetails)
            .HasForeignKey(detail => detail.ProcurementId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(detail => detail.Vendor)
            .WithMany()
            .HasForeignKey(detail => detail.VendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> entity)
    {
        entity.HasKey(vendor => vendor.VendorId);
        entity.Property(vendor => vendor.VendorId).ValueGeneratedNever();
        entity.Property(vendor => vendor.VendorCode).IsRequired();
        entity.HasIndex(vendor => vendor.VendorCode).IsUnique().HasFilter("[IsDeleted] = 0");
        entity.HasMany(v => v.VendorOffers).WithOne(vo => vo.Vendor).HasForeignKey(vo => vo.VendorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class VendorOfferConfiguration : IEntityTypeConfiguration<VendorOffer>
{
    public void Configure(EntityTypeBuilder<VendorOffer> entity)
    {
        entity.Property(vendorOffer => vendorOffer.VendorOfferId).ValueGeneratedNever();
        entity.HasOne(vo => vo.Procurement).WithMany(p => p.VendorOffers).HasForeignKey(vo => vo.ProcurementId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(vo => vo.ProcOffer).WithMany().HasForeignKey(vo => vo.ProcOfferId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(vo => vo.Vendor).WithMany(v => v.VendorOffers).HasForeignKey(vo => vo.VendorId).OnDelete(DeleteBehavior.Cascade);
    }
}
