using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data.Configurations;

public sealed class LdpArchiveRowConfiguration : IEntityTypeConfiguration<LdpArchiveRow>
{
    public void Configure(EntityTypeBuilder<LdpArchiveRow> entity)
    {
        entity.Property(row => row.LdpArchiveRowId).ValueGeneratedNever();
        entity.HasIndex(row => row.ImportBatch);
        entity.HasIndex(row => row.SourceKey).IsUnique().HasFilter("[SourceKey] IS NOT NULL");
    }
}
