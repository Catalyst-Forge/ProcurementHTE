using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User, Role, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Status> Statuses { get; set; } = null!;
    public DbSet<Procurement> Procurements { get; set; } = null!;
    public DbSet<JobTypes> JobTypes { get; set; } = null!;
    public DbSet<JobTypeDocuments> JobTypeDocuments { get; set; } = null!;
    public DbSet<ProcDetail> ProcDetails { get; set; } = null!;
    public DbSet<ProcOffer> ProcOffers { get; set; } = null!;
    public DbSet<ProcDocuments> ProcDocuments { get; set; } = null!;
    public DbSet<ProcDocumentApprovals> ProcDocumentApprovals { get; set; } = null!;
    public DbSet<Vendor> Vendors { get; set; } = null!;
    public DbSet<VendorOffer> VendorOffers { get; set; } = null!;
    public DbSet<ProfitLoss> ProfitLosses { get; set; } = null!;
    public DbSet<ProfitLossItem> ProfitLossItems { get; set; } = null!;
    public DbSet<ProfitLossSelectedVendor> ProfitLossSelectedVendors { get; set; } = null!;
    public DbSet<DocumentApprovals> DocumentApprovals { get; set; } = null!;
    public DbSet<DocumentType> DocumentTypes { get; set; } = null!;
    public DbSet<Tender> Tenders { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder
            .Entity<User>()
            .Property(u => u.FullName)
            .HasComputedColumnSql("CONCAT([FirstName], ' ', [LastName])");

        builder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });

        ConfigureProcurement(builder);
        ConfigureVendors(builder);
        ConfigureDocuments(builder);
        ConfigureProfitLoss(builder);
        ConfigureJobTypeDocuments(builder);
    }

    private static void ConfigureProcurement(ModelBuilder builder)
    {
        builder.Entity<Procurement>(entity =>
        {
            entity.HasKey(p => p.ProcurementId);
            entity.Property(p => p.ProcNum).IsRequired();
            entity.HasIndex(p => p.ProcNum).IsUnique().HasDatabaseName("AK_Procurements_ProcNum");
            entity
                .HasIndex(p => new { p.UserId, p.CreatedAt })
                .HasDatabaseName("IX_Procurements_UserId_CreatedAt")
                .IsDescending(false, true);

            entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        builder.Entity<JobTypes>()
            .Property(j => j.JobTypeId)
            .ValueGeneratedOnAdd();

        builder.Entity<Procurement>()
            .Property(p => p.ProcurementId)
            .ValueGeneratedOnAdd();

        builder.Entity<Procurement>()
            .HasOne(p => p.Status)
            .WithMany()
            .HasForeignKey(p => p.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Procurement>()
            .HasOne(p => p.User)
            .WithMany(u => u.Procurements)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Procurement>()
            .HasOne(p => p.JobTypeConfig)
            .WithMany(j => j.Procurements)
            .HasForeignKey(p => p.JobTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Procurement>()
            .HasOne(p => p.PicOpsUser)
            .WithMany()
            .HasForeignKey(p => p.PicOpsUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Procurement>()
            .HasOne(p => p.AnalystHteSignerUser)
            .WithMany()
            .HasForeignKey(p => p.AnalystHteSignerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Procurement>()
            .HasOne(p => p.AssistantManagerSignerUser)
            .WithMany()
            .HasForeignKey(p => p.AssistantManagerSignerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Procurement>()
            .HasOne(p => p.ManagerSignerUser)
            .WithMany()
            .HasForeignKey(p => p.ManagerSignerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProcDetail>()
            .Property(d => d.ProcDetailId)
            .ValueGeneratedOnAdd();

        builder.Entity<ProcDetail>()
            .HasOne(d => d.Procurement)
            .WithMany(p => p.ProcDetails)
            .HasForeignKey(d => d.ProcurementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProcDetail>()
            .HasOne(d => d.Vendor)
            .WithMany()
            .HasForeignKey(d => d.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProcOffer>()
            .Property(o => o.ProcOfferId)
            .ValueGeneratedOnAdd();

        builder.Entity<ProcOffer>()
            .HasOne(o => o.Procurement)
            .WithMany(p => p.ProcOffers)
            .HasForeignKey(o => o.ProcurementId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureVendors(ModelBuilder builder)
    {
        builder.Entity<Vendor>(entity =>
        {
            entity.HasKey(v => v.VendorId);
            entity.Property(v => v.VendorCode).IsRequired();
            entity.HasIndex(v => v.VendorCode).IsUnique();
        });

        builder.Entity<Vendor>()
            .Property(v => v.VendorId)
            .ValueGeneratedOnAdd();

        builder.Entity<VendorOffer>()
            .Property(vo => vo.VendorOfferId)
            .ValueGeneratedOnAdd();

        builder.Entity<VendorOffer>()
            .HasOne(vo => vo.Procurement)
            .WithMany(p => p.VendorOffers)
            .HasForeignKey(vo => vo.ProcurementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<VendorOffer>()
            .HasOne(vo => vo.ProcOffer)
            .WithMany()
            .HasForeignKey(vo => vo.ProcOfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<VendorOffer>()
            .HasOne(vo => vo.Vendor)
            .WithMany(v => v.VendorOffers)
            .HasForeignKey(vo => vo.VendorId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureDocuments(ModelBuilder builder)
    {
        builder.Entity<DocumentType>()
            .Property(d => d.DocumentTypeId)
            .ValueGeneratedOnAdd();

        builder.Entity<ProcDocuments>()
            .Property(d => d.ProcDocumentId)
            .ValueGeneratedOnAdd();

        builder.Entity<ProcDocuments>()
            .HasOne(d => d.Procurement)
            .WithMany(p => p.ProcDocuments)
            .HasForeignKey(d => d.ProcurementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProcDocuments>()
            .HasOne(d => d.DocumentType)
            .WithMany(dt => dt.ProcDocuments)
            .HasForeignKey(d => d.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProcDocumentApprovals>()
            .Property(a => a.ProcDocumentApprovalId)
            .ValueGeneratedOnAdd();

        builder.Entity<ProcDocumentApprovals>()
            .HasOne(a => a.Procurement)
            .WithMany(p => p.DocumentApprovals)
            .HasForeignKey(a => a.ProcurementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProcDocumentApprovals>()
            .HasOne(a => a.ProcDocument)
            .WithMany(d => d.Approvals)
            .HasForeignKey(a => a.ProcDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProcDocumentApprovals>()
            .HasOne(a => a.Role)
            .WithMany()
            .HasForeignKey(a => a.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProcDocumentApprovals>()
            .HasOne(a => a.Approver)
            .WithMany()
            .HasForeignKey(a => a.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureProfitLoss(ModelBuilder builder)
    {
        builder.Entity<ProfitLoss>()
            .Property(p => p.ProfitLossId)
            .ValueGeneratedOnAdd();

        builder.Entity<ProfitLoss>()
            .HasOne(pl => pl.Procurement)
            .WithMany(p => p.ProfitLosses)
            .HasForeignKey(pl => pl.ProcurementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProfitLoss>()
            .HasOne(pl => pl.SelectedVendor)
            .WithMany()
            .HasForeignKey(pl => pl.SelectedVendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProfitLossItem>()
            .Property(i => i.ProfitLossItemId)
            .ValueGeneratedOnAdd();

        builder.Entity<ProfitLossItem>()
            .HasOne(i => i.ProfitLoss)
            .WithMany(pl => pl.Items)
            .HasForeignKey(i => i.ProfitLossId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProfitLossItem>()
            .HasOne(i => i.ProcOffer)
            .WithMany()
            .HasForeignKey(i => i.ProcOfferId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureJobTypeDocuments(ModelBuilder builder)
    {
        builder.Entity<JobTypeDocuments>()
            .Property(j => j.JobTypeDocumentId)
            .ValueGeneratedOnAdd();

        builder.Entity<JobTypeDocuments>()
            .HasOne(j => j.JobType)
            .WithMany(jt => jt.JobTypeDocuments)
            .HasForeignKey(j => j.JobTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<JobTypeDocuments>()
            .HasOne(j => j.DocumentType)
            .WithMany(dt => dt.JobTypeDocuments)
            .HasForeignKey(j => j.DocumentTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DocumentApprovals>()
            .Property(da => da.DocumentApprovalId)
            .ValueGeneratedOnAdd();

        builder.Entity<DocumentApprovals>()
            .HasOne(da => da.JobTypeDocument)
            .WithMany(j => j.DocumentApprovals)
            .HasForeignKey(da => da.JobTypeDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DocumentApprovals>()
            .HasOne(da => da.Role)
            .WithMany()
            .HasForeignKey(da => da.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
