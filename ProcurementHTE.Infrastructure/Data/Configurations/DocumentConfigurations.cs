using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data.Configurations;

public sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> entity)
    {
        entity.Property(documentType => documentType.DocumentTypeId).ValueGeneratedNever();
        entity.HasMany(dt => dt.ProcDocuments).WithOne(d => d.DocumentType).HasForeignKey(d => d.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(dt => dt.JobTypeDocuments).WithOne(jtd => jtd.DocumentType).HasForeignKey(jtd => jtd.DocumentTypeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProcDocumentsConfiguration : IEntityTypeConfiguration<ProcDocuments>
{
    public void Configure(EntityTypeBuilder<ProcDocuments> entity)
    {
        entity.Property(document => document.ProcDocumentId).ValueGeneratedNever();
        entity.HasOne(d => d.Procurement).WithMany(p => p.ProcDocuments).HasForeignKey(d => d.ProcurementId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(d => d.DocumentType).WithMany(dt => dt.ProcDocuments).HasForeignKey(d => d.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class JobTypeDocumentsConfiguration : IEntityTypeConfiguration<JobTypeDocuments>
{
    public void Configure(EntityTypeBuilder<JobTypeDocuments> entity)
    {
        entity.Property(jobTypeDoc => jobTypeDoc.JobTypeDocumentId).ValueGeneratedNever();
        entity.HasOne(jtd => jtd.JobType).WithMany(jt => jt.JobTypeDocuments).HasForeignKey(jtd => jtd.JobTypeId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(jtd => jtd.DocumentType).WithMany(dt => dt.JobTypeDocuments).HasForeignKey(jtd => jtd.DocumentTypeId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(jtd => jtd.DocumentApprovals).WithOne(da => da.JobTypeDocument).HasForeignKey(da => da.JobTypeDocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DocumentApprovalsConfiguration : IEntityTypeConfiguration<DocumentApprovals>
{
    public void Configure(EntityTypeBuilder<DocumentApprovals> entity)
    {
        entity.Property(documentApproval => documentApproval.DocumentApprovalId).ValueGeneratedNever();
        entity.HasOne(da => da.JobTypeDocument).WithMany(jtd => jtd.DocumentApprovals).HasForeignKey(da => da.JobTypeDocumentId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(da => da.Role).WithMany().HasForeignKey(da => da.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}
