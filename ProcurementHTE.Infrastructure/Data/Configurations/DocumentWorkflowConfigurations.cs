using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data.Configurations;

public sealed class VendorRoundLetterConfiguration : IEntityTypeConfiguration<VendorRoundLetter>
{
    public void Configure(EntityTypeBuilder<VendorRoundLetter> entity)
    {
        entity.Property(x => x.VendorRoundLetterId).ValueGeneratedNever();
        entity.HasOne(x => x.ProcDocument).WithMany().HasForeignKey(x => x.ProcDocumentId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.ProcurementId, x.VendorId, x.Round }).IsUnique();
    }
}

public sealed class DocumentApprovalRuleConfiguration : IEntityTypeConfiguration<DocumentApprovalRule>
{
    public void Configure(EntityTypeBuilder<DocumentApprovalRule> entity)
    {
        entity.HasKey(r => r.DocumentApprovalRuleId);
        entity.Property(r => r.MinAmount).HasPrecision(18, 2);
        entity.Property(r => r.MaxAmount).HasPrecision(18, 2);
        entity.Property(r => r.Sequence).HasDefaultValue(1);
        entity.Property(r => r.IsActive).HasDefaultValue(true);
        entity.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        entity.HasOne(r => r.DocumentType).WithMany().HasForeignKey(r => r.DocumentTypeId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(r => r.JobType).WithMany().HasForeignKey(r => r.JobTypeId).OnDelete(DeleteBehavior.NoAction);
        entity.HasIndex(r => new
        {
            r.DocumentTypeId,
            r.JobTypeId,
            r.ProcurementCategory,
            r.MinAmount,
            r.MaxAmount,
            r.IsActive,
        });
    }
}
