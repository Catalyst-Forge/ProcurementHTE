using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User, Role, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

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

        // ========================================
        // IDENTITY & USER CONFIGURATION
        // ========================================
        ConfigureUser(builder);
        ConfigureUserRole(builder);

        // ========================================
        // ENTITY CONFIGURATIONS
        // ========================================
        ConfigureProcurement(builder);
        ConfigureJobType(builder);
        ConfigureProcDetail(builder);
        ConfigureVendors(builder);
        ConfigureVendorOffer(builder);
        ConfigureDocumentType(builder);
        ConfigureProcDocuments(builder);
        ConfigureProcDocumentApprovals(builder);
        ConfigureProfitLoss(builder);
        ConfigureProfitLossItem(builder);
        ConfigureJobTypeDocuments(builder);
        ConfigureDocumentApprovals(builder);
    }

    #region Identity & User

    private static void ConfigureUser(ModelBuilder builder)
    {
        builder
            .Entity<User>()
            .Property(u => u.FullName)
            .HasComputedColumnSql("CONCAT([FirstName], ' ', [LastName])");
    }

    private static void ConfigureUserRole(ModelBuilder builder)
    {
        builder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
    }

    #endregion

    #region Procurement
    private static void ConfigureProcurement(ModelBuilder builder)
    {
        builder.Entity<Procurement>(entity =>
        {
            // Primary key
            entity.HasKey(procurement => procurement.ProcurementId);
            // Properties
            entity.Property(procurement => procurement.ProcurementId).ValueGeneratedOnAdd();
            entity.Property(procurement => procurement.ProcNum).IsRequired();
            entity
                .Property(procurement => procurement.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Indexes
            entity
                .HasIndex(procurement => procurement.ProcNum)
                .IsUnique()
                .HasDatabaseName("AK_Procurements_ProcNum");
            entity
                .HasIndex(procurement => new { procurement.UserId, procurement.CreatedAt })
                .HasDatabaseName("IX_Procurements_UserId_CreatedAt")
                .IsDescending(false, true);

            // Relationships
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

            entity
                .HasMany(procurement => procurement.ProcDetails)
                .WithOne(detail => detail.Procurement)
                .HasForeignKey(detail => detail.ProcurementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(procurement => procurement.ProcOffers)
                .WithOne(offer => offer.Procurement)
                .HasForeignKey(offer => offer.ProcurementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(procurement => procurement.ProcDocuments)
                .WithOne(document => document.Procurement)
                .HasForeignKey(document => document.ProcurementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(procurement => procurement.VendorOffers)
                .WithOne(vendorOffer => vendorOffer.Procurement)
                .HasForeignKey(vendorOffer => vendorOffer.ProcurementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(procurement => procurement.ProfitLosses)
                .WithOne(profitLoss => profitLoss.Procurement)
                .HasForeignKey(profitLoss => profitLoss.ProcurementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(procurement => procurement.DocumentApprovals)
                .WithOne(documentApproval => documentApproval.Procurement)
                .HasForeignKey(documentApproval => documentApproval.ProcurementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder
            .Entity<ProcOffer>()
            .Property(procOffere => procOffere.ProcOfferId)
            .ValueGeneratedOnAdd();
    }

    #endregion

    #region Jobtype

    private static void ConfigureJobType(ModelBuilder builder)
    {
        builder.Entity<JobTypes>(entity =>
        {
            // Properties
            entity.Property(job => job.JobTypeId).ValueGeneratedOnAdd();

            // Relationships
            entity
                .HasMany(job => job.Procurements)
                .WithOne(procurement => procurement.JobType)
                .HasForeignKey(procurement => procurement.JobTypeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity
                .HasMany(job => job.JobTypeDocuments)
                .WithOne(jobTypeDoc => jobTypeDoc.JobType)
                .HasForeignKey(jobTypeDoc => jobTypeDoc.JobTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    #endregion

    #region ProcDetail

    private static void ConfigureProcDetail(ModelBuilder builder)
    {
        builder.Entity<ProcDetail>(entity =>
        {
            // Properties
            entity.Property(detail => detail.ProcDetailId).ValueGeneratedOnAdd();

            // Relationships
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
        });
    }

    #endregion

    #region Vendor

    private static void ConfigureVendors(ModelBuilder builder)
    {
        builder.Entity<Vendor>(entity =>
        {
            // Primary Key
            entity.HasKey(vendor => vendor.VendorId);
            // Properties
            entity.Property(vendor => vendor.VendorId).ValueGeneratedOnAdd();
            entity.Property(vendor => vendor.VendorCode).IsRequired();

            // Indexes
            entity.HasIndex(vendor => vendor.VendorCode).IsUnique();

            // Relationships
            entity
                .HasMany(v => v.VendorOffers)
                .WithOne(vo => vo.Vendor)
                .HasForeignKey(vo => vo.VendorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    #endregion

    #region VendorOffer

    private static void ConfigureVendorOffer(ModelBuilder builder)
    {
        builder.Entity<VendorOffer>(entity =>
        {
            // Properties
            entity.Property(vendorOffer => vendorOffer.VendorOfferId).ValueGeneratedOnAdd();

            // Relationships
            entity
                .HasOne(vendorOffer => vendorOffer.Procurement)
                .WithMany(procurement => procurement.VendorOffers)
                .HasForeignKey(vendorOffer => vendorOffer.ProcurementId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(vendorOffer => vendorOffer.ProcOffer)
                .WithMany()
                .HasForeignKey(vendorOffer => vendorOffer.ProcOfferId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(vendorOffer => vendorOffer.Vendor)
                .WithMany(vendor => vendor.VendorOffers)
                .HasForeignKey(vendorOffer => vendorOffer.VendorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    #endregion

    #region DocumentType

    private static void ConfigureDocumentType(ModelBuilder builder) {
        builder.Entity<DocumentType>(entity => {
            // Properties
            entity.Property(documentType => documentType.DocumentTypeId).ValueGeneratedOnAdd();

            // Relationships
            entity.HasMany(documentType => documentType.ProcDocuments)
                .WithOne(document => document.DocumentType)
                .HasForeignKey(document => document.DocumentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(documentType => documentType.JobTypeDocuments)
                .WithOne(jobTypeDoc => jobTypeDoc.DocumentType)
                .HasForeignKey(jobTypeDoc => jobTypeDoc.DocumentTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    #endregion

    #region ProcDocuments

    private static void ConfigureProcDocuments(ModelBuilder builder) {
        builder.Entity<ProcDocuments>(entity => {
            // Properties
            entity.Property(document => document.ProcDocumentId).ValueGeneratedOnAdd();

            // Relationships
            entity.HasOne(document => document.Procurement)
                .WithMany(procurement => procurement.ProcDocuments)
                .HasForeignKey(document => document.ProcurementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(document => document.DocumentType)
                .WithMany(documentType => documentType.ProcDocuments)
                .HasForeignKey(document => document.DocumentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(document => document.Approvals)
                .WithOne(approval => approval.ProcDocument)
                .HasForeignKey(approval => approval.ProcDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    #endregion

    #region ProcDocumentApprovals

    private static void ConfigureProcDocumentApprovals(ModelBuilder builder) {
        builder.Entity<ProcDocumentApprovals>(entity => {
            // Properties
            entity.Property(documentApproval => documentApproval.ProcDocumentApprovalId).ValueGeneratedOnAdd();

            // Relationships
            entity.HasOne(documentApproval => documentApproval.Procurement)
                .WithMany(procurement => procurement.DocumentApprovals)
                .HasForeignKey(documentApproval => documentApproval.ProcurementId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(documentApproval => documentApproval.ProcDocument)
                .WithMany(document => document.Approvals)
                .HasForeignKey(documentApproval => documentApproval.ProcDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(documentApproval => documentApproval.Role)
                .WithMany()
                .HasForeignKey(documentApproval => documentApproval.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(documentApproval => documentApproval.Approver)
                .WithMany()
                .HasForeignKey(documentApproval => documentApproval.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    #endregion

    #region ProfitLoss

    private static void ConfigureProfitLoss(ModelBuilder builder) {
        builder.Entity<ProfitLoss>(entity => {
            // Properties
            entity.Property(profitLoss => profitLoss.ProfitLossId).ValueGeneratedOnAdd();

            // Relationships
            entity.HasOne(profitLoss => profitLoss.Procurement)
                .WithMany(procurement => procurement.ProfitLosses)
                .HasForeignKey(profitLoss => profitLoss.ProcurementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(profitLoss => profitLoss.SelectedVendor)
                .WithMany()
                .HasForeignKey(profitLoss => profitLoss.SelectedVendorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(profitLoss => profitLoss.Items)
                .WithOne(item => item.ProfitLoss)
                .HasForeignKey(item => item.ProfitLossId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    #endregion

    #region ProfitLossItem

    private static void ConfigureProfitLossItem(ModelBuilder builder) {
        builder.Entity<ProfitLossItem>(entity => {
            // Properties
            entity.Property(item => item.ProfitLossItemId).ValueGeneratedOnAdd();

            // Relationships
            entity.HasOne(item => item.ProfitLoss)
                .WithMany(profitLoss => profitLoss.Items)
                .HasForeignKey(item => item.ProfitLossId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.ProcOffer)
                .WithMany()
                .HasForeignKey(item => item.ProcOfferId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    #endregion

    #region JobTypeDocuments

    private static void ConfigureJobTypeDocuments(ModelBuilder builder) {
        builder.Entity<JobTypeDocuments>(entity => {
            // Properties
            entity.Property(jobTypeDoc => jobTypeDoc.JobTypeDocumentId).ValueGeneratedOnAdd();

            // Relationships
            entity.HasOne(jobTypeDoc => jobTypeDoc.JobType)
                .WithMany(jobType => jobType.JobTypeDocuments)
                .HasForeignKey(jobTypeDoc => jobTypeDoc.JobTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(jobTypeDoc => jobTypeDoc.DocumentType)
                .WithMany(documentType => documentType.JobTypeDocuments)
                .HasForeignKey(jobTypeDoc => jobTypeDoc.DocumentTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(jobTypeDoc => jobTypeDoc.DocumentApprovals)
                .WithOne(documentApproval => documentApproval.JobTypeDocument)
                .HasForeignKey(documentApproval => documentApproval.JobTypeDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    #endregion

    #region DocumentApprovals

    private static void ConfigureDocumentApprovals(ModelBuilder builder) {
        builder.Entity<DocumentApprovals>(entity => {
            // Properties
            entity.Property(documentApproval => documentApproval.DocumentApprovalId).ValueGeneratedOnAdd();

            // Relationships
            entity.HasOne(documentApproval => documentApproval.JobTypeDocument)
                .WithMany(jobTypeDoc => jobTypeDoc.DocumentApprovals)
                .HasForeignKey(documentApproval => documentApproval.JobTypeDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(documentApproval => documentApproval.Role)
                .WithMany()
                .HasForeignKey(documentApproval => documentApproval.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    #endregion
}
