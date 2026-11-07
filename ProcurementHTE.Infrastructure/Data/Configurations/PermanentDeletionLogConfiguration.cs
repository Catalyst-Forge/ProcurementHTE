using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data.Configurations;

public sealed class PermanentDeletionLogConfiguration : IEntityTypeConfiguration<PermanentDeletionLog>
{
    public void Configure(EntityTypeBuilder<PermanentDeletionLog> entity)
    {
        entity.ToTable(table => table.HasTrigger(PermanentDeletionLog.AppendOnlyTrigger));
        entity.Property(log => log.PermanentDeletionLogId).ValueGeneratedNever();
        entity.HasIndex(log => log.DeletedAt);
    }
}
