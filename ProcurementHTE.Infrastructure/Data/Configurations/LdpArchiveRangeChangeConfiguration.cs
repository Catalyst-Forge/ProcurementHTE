using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data.Configurations;

public sealed class LdpArchiveRangeChangeConfiguration : IEntityTypeConfiguration<LdpArchiveRangeChange>
{
    public void Configure(EntityTypeBuilder<LdpArchiveRangeChange> entity)
    {
        entity.ToTable(table => table.HasTrigger(LdpArchiveRangeChange.AppendOnlyTrigger));
        entity.Property(change => change.LdpArchiveRangeChangeId).ValueGeneratedNever();
        entity.Property(change => change.StartFrom).HasColumnType("date");
        entity.Property(change => change.StartTo).HasColumnType("date");
        entity.Property(change => change.PreviousStartFrom).HasColumnType("date");
        entity.Property(change => change.PreviousStartTo).HasColumnType("date");
        entity.HasIndex(change => change.ChangedAt);
    }
}
