using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data.Configurations;

public sealed class ProfitLossConfiguration : IEntityTypeConfiguration<ProfitLoss>
{
    public void Configure(EntityTypeBuilder<ProfitLoss> entity)
    {
        entity.Property(profitLoss => profitLoss.ProfitLossId).ValueGeneratedNever();

        entity
            .HasOne(profitLoss => profitLoss.Procurement)
            .WithMany(procurement => procurement.ProfitLosses)
            .HasForeignKey(profitLoss => profitLoss.ProcurementId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasMany(profitLoss => profitLoss.VendorOffers)
            .WithOne(vendorOffer => vendorOffer.ProfitLoss)
            .HasForeignKey(vendorOffer => vendorOffer.ProfitLossId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasMany(profitLoss => profitLoss.Items)
            .WithOne(item => item.ProfitLoss)
            .HasForeignKey(item => item.ProfitLossId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProfitLossItemConfiguration : IEntityTypeConfiguration<ProfitLossItem>
{
    public void Configure(EntityTypeBuilder<ProfitLossItem> entity)
    {
        entity.Property(item => item.ProfitLossItemId).ValueGeneratedNever();
        entity.HasOne(item => item.ProfitLoss).WithMany(pl => pl.Items).HasForeignKey(item => item.ProfitLossId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(item => item.ProcOffer).WithMany().HasForeignKey(item => item.ProcOfferId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UnitTypeConfiguration : IEntityTypeConfiguration<UnitType>
{
    public void Configure(EntityTypeBuilder<UnitType> entity)
    {
        entity.Property(unitType => unitType.UnitTypeId).ValueGeneratedNever();
        entity.Property(unitType => unitType.Code).IsRequired().HasMaxLength(20);
        entity.Property(unitType => unitType.Name).IsRequired().HasMaxLength(50);
        entity.Property(unitType => unitType.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        entity.HasIndex(unitType => unitType.Code).IsUnique();
        entity.HasMany(unitType => unitType.ProfitLossItems).WithOne(item => item.UnitType).HasForeignKey(item => item.UnitTypeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(unitType => unitType.VendorOffers).WithOne(offer => offer.UnitType).HasForeignKey(offer => offer.UnitTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
