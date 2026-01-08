using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<User, Role, string>(options)
    {
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Procurement> Procurements { get; set; }
        public DbSet<PurchaseRequisition> PurchaseRequisitions { get; set; }
        public DbSet<PurchaseRequisitionStatusHistory> PurchaseRequisitionStatusHistories { get; set; }
        public DbSet<JobTypes> JobTypes { get; set; }
        public DbSet<JobTypeDocuments> JobTypeDocuments { get; set; }
        public DbSet<ProcDetail> ProcDetails { get; set; }
        public DbSet<ProcOffer> ProcOffers { get; set; }
        public DbSet<ProcDocuments> ProcDocuments { get; set; }

        // ProcDocumentApprovals removed - approval sekarang di level PR
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<VendorOffer> VendorOffers { get; set; }
        public DbSet<ProfitLoss> ProfitLosses { get; set; }
        public DbSet<ProfitLossItem> ProfitLossItems { get; set; }
        public DbSet<ProfitLossSelectedVendor> ProfitLossSelectedVendors { get; set; }
        public DbSet<DocumentApprovals> DocumentApprovals { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<VendorRoundLetter> VendorRoundLetters { get; set; }
        public DbSet<DocumentApprovalRule> DocumentApprovalRules { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<UserSecurityLog> UserSecurityLogs { get; set; }
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<UnitType> UnitTypes { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ========================================
            // IDENTITY & USER CONFIGURATION
            // ========================================
            ConfigureUser(builder);
            ConfigureUserRole(builder);
            ConfigureUserSession(builder);
            ConfigureUserSecurityLog(builder);

            // ========================================
            // ENTITY CONFIGURATIONS
            // ========================================
            ConfigurePurchaseRequisition(builder);
            ConfigureProcurement(builder);
            ConfigureJobType(builder);
            ConfigureProcDetail(builder);
            ConfigureVendors(builder);
            ConfigureVendorOffer(builder);
            ConfigureDocumentType(builder);
            ConfigureProcDocuments(builder);
            // ConfigureProcDocumentApprovals removed - approval sekarang di level PR
            ConfigureProfitLoss(builder);
            ConfigureProfitLossItem(builder);
            ConfigureJobTypeDocuments(builder);
            ConfigureDocumentApprovals(builder);
            ConfigureVendorRoundLetters(builder);
            ConfigureDocumentApprovalRules(builder);
            ConfigureUnitType(builder);
            ConfigureNotification(builder);

            // ========================================
            // SEED DATA
            // ========================================
            UnitTypeSeeder.SeedUnitTypes(builder);

            // ========================================
            // GLOBAL QUERY FILTERS (SOFT DELETE)
            // ========================================
            ConfigureGlobalQueryFilters(builder);
        }

        private static void ConfigureGlobalQueryFilters(ModelBuilder builder)
        {
            // Apply soft delete filter for all entities inheriting from BaseEntity
            // Note: Child entities (ProcDetail, ProcOffer, etc.) don't have IsDeleted
            // They are cascade deleted when parent is deleted, so this is expected behavior
            builder.Entity<Procurement>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<ProfitLoss>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<VendorOffer>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<ProcDocuments>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<PurchaseRequisition>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<Vendor>().HasQueryFilter(e => !e.IsDeleted);
        }

        #region Identity & User

        private static void ConfigureUser(ModelBuilder builder)
        {
            builder
                .Entity<User>()
                .Property(u => u.FullName)
                .HasComputedColumnSql("CONCAT([FirstName], ' ', [LastName])");

            builder
                .Entity<User>()
                .Property(u => u.TwoFactorMethod)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(TwoFactorMethod.None);

            builder
                .Entity<User>()
                .HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .Entity<User>()
                .HasMany(u => u.SecurityLogs)
                .WithOne(log => log.User)
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureUserRole(ModelBuilder builder)
        {
            builder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
        }

        private static void ConfigureUserSession(ModelBuilder builder)
        {
            builder.Entity<UserSession>(entity =>
            {
                entity.HasKey(session => session.UserSessionId);
                entity.Property(session => session.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity
                    .Property(session => session.LastAccessedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });
        }

        private static void ConfigureUserSecurityLog(ModelBuilder builder)
        {
            builder.Entity<UserSecurityLog>(entity =>
            {
                entity.HasKey(log => log.UserSecurityLogId);
                entity.Property(log => log.EventType).HasConversion<string>().HasMaxLength(50);
                entity.Property(log => log.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }

        #endregion

        #region PurchaseRequisition

        private static void ConfigurePurchaseRequisition(ModelBuilder builder)
        {
            builder.Entity<PurchaseRequisition>(entity =>
            {
                // Primary key
                entity.HasKey(pr => pr.PrId);

                // Properties
                entity.Property(pr => pr.PrId).ValueGeneratedNever();
                entity.Property(pr => pr.PrNumber).IsRequired().HasMaxLength(100);
                entity.Property(pr => pr.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity
                    .HasIndex(pr => pr.PrNumber)
                    .IsUnique()
                    .HasDatabaseName("AK_PurchaseRequisitions_PrNumber");

                // Relationships
                entity
                    .HasOne(pr => pr.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(pr => pr.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity
                    .HasMany(pr => pr.Procurements)
                    .WithOne(p => p.PurchaseRequisition)
                    .HasForeignKey(p => p.PrId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
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
                entity.Property(procurement => procurement.ProcurementId).ValueGeneratedNever();
                entity.Property(procurement => procurement.ProcNum).IsRequired();
                entity
                    .Property(procurement => procurement.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity
                    .Property(procurement => procurement.ContractType)
                    .HasConversion<string>()
                    .HasMaxLength(50);
                entity
                    .Property(procurement => procurement.ProjectRegion)
                    .HasConversion<string>()
                    .HasMaxLength(50);
                entity
                    .Property(procurement => procurement.ProcurementCategory)
                    .HasConversion<string>()
                    .HasMaxLength(50);

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

                // DocumentApprovals navigation removed - approval per-document sudah dihapus
            });

            builder
                .Entity<ProcOffer>()
                .Property(procOffer => procOffer.ProcOfferId)
                .ValueGeneratedNever();
        }

        #endregion

        #region Jobtype

        private static void ConfigureJobType(ModelBuilder builder)
        {
            builder.Entity<JobTypes>(entity =>
            {
                // Properties
                entity.Property(job => job.JobTypeId).ValueGeneratedNever();

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
                entity.Property(detail => detail.ProcDetailId).ValueGeneratedNever();

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
                entity.Property(vendor => vendor.VendorId).ValueGeneratedNever();
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
                entity.Property(vendorOffer => vendorOffer.VendorOfferId).ValueGeneratedNever();

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

        private static void ConfigureDocumentType(ModelBuilder builder)
        {
            builder.Entity<DocumentType>(entity =>
            {
                // Properties
                entity.Property(documentType => documentType.DocumentTypeId).ValueGeneratedNever();

                // Relationships
                entity
                    .HasMany(documentType => documentType.ProcDocuments)
                    .WithOne(document => document.DocumentType)
                    .HasForeignKey(document => document.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity
                    .HasMany(documentType => documentType.JobTypeDocuments)
                    .WithOne(jobTypeDoc => jobTypeDoc.DocumentType)
                    .HasForeignKey(jobTypeDoc => jobTypeDoc.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        #endregion

        #region ProcDocuments

        private static void ConfigureProcDocuments(ModelBuilder builder)
        {
            builder.Entity<ProcDocuments>(entity =>
            {
                // Properties
                entity.Property(document => document.ProcDocumentId).ValueGeneratedNever();

                // Relationships
                entity
                    .HasOne(document => document.Procurement)
                    .WithMany(procurement => procurement.ProcDocuments)
                    .HasForeignKey(document => document.ProcurementId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasOne(document => document.DocumentType)
                    .WithMany(documentType => documentType.ProcDocuments)
                    .HasForeignKey(document => document.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Approvals navigation removed - approval per-document sudah dihapus
            });
        }

        #endregion

        // #region ProcDocumentApprovals - REMOVED
        // Approval per document sudah dihapus, approval sekarang di level PR

        #region ProfitLoss

        private static void ConfigureProfitLoss(ModelBuilder builder)
        {
            builder.Entity<ProfitLoss>(entity =>
            {
                // Properties
                entity.Property(profitLoss => profitLoss.ProfitLossId).ValueGeneratedNever();

                // Relationships
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
            });
        }

        #endregion

        #region ProfitLossItem

        private static void ConfigureProfitLossItem(ModelBuilder builder)
        {
            builder.Entity<ProfitLossItem>(entity =>
            {
                // Properties
                entity.Property(item => item.ProfitLossItemId).ValueGeneratedNever();

                // Relationships
                entity
                    .HasOne(item => item.ProfitLoss)
                    .WithMany(profitLoss => profitLoss.Items)
                    .HasForeignKey(item => item.ProfitLossId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity
                    .HasOne(item => item.ProcOffer)
                    .WithMany()
                    .HasForeignKey(item => item.ProcOfferId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        #endregion

        #region VendorRoundLetters
        private static void ConfigureVendorRoundLetters(ModelBuilder builder)
        {
            builder.Entity<VendorRoundLetter>(entity =>
            {
                entity.Property(x => x.VendorRoundLetterId).ValueGeneratedNever();

                entity
                    .HasOne(x => x.ProcDocument)
                    .WithMany()
                    .HasForeignKey(x => x.ProcDocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasOne(x => x.Vendor)
                    .WithMany()
                    .HasForeignKey(x => x.VendorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity
                    .HasIndex(x => new
                    {
                        x.ProcurementId,
                        x.VendorId,
                        x.Round,
                    })
                    .IsUnique();
            });
        }
        #endregion

        #region JobTypeDocuments

        private static void ConfigureJobTypeDocuments(ModelBuilder builder)
        {
            builder.Entity<JobTypeDocuments>(entity =>
            {
                // Properties
                // Use client-generated GUIDs; do not expect the database to generate the key.
                entity.Property(jobTypeDoc => jobTypeDoc.JobTypeDocumentId).ValueGeneratedNever();

                // Relationships
                entity
                    .HasOne(jobTypeDoc => jobTypeDoc.JobType)
                    .WithMany(jobType => jobType.JobTypeDocuments)
                    .HasForeignKey(jobTypeDoc => jobTypeDoc.JobTypeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasOne(jobTypeDoc => jobTypeDoc.DocumentType)
                    .WithMany(documentType => documentType.JobTypeDocuments)
                    .HasForeignKey(jobTypeDoc => jobTypeDoc.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasMany(jobTypeDoc => jobTypeDoc.DocumentApprovals)
                    .WithOne(documentApproval => documentApproval.JobTypeDocument)
                    .HasForeignKey(documentApproval => documentApproval.JobTypeDocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        #endregion

        #region DocumentApprovals

        private static void ConfigureDocumentApprovals(ModelBuilder builder)
        {
            builder.Entity<DocumentApprovals>(entity =>
            {
                // Properties
                entity
                    .Property(documentApproval => documentApproval.DocumentApprovalId)
                    .ValueGeneratedNever();

                // Relationships
                entity
                    .HasOne(documentApproval => documentApproval.JobTypeDocument)
                    .WithMany(jobTypeDoc => jobTypeDoc.DocumentApprovals)
                    .HasForeignKey(documentApproval => documentApproval.JobTypeDocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasOne(documentApproval => documentApproval.Role)
                    .WithMany()
                    .HasForeignKey(documentApproval => documentApproval.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        #endregion

        #region UnitType

        private static void ConfigureUnitType(ModelBuilder builder)
        {
            builder.Entity<UnitType>(entity =>
            {
                // Properties
                entity.Property(unitType => unitType.UnitTypeId).ValueGeneratedNever();
                entity.Property(unitType => unitType.Code).IsRequired().HasMaxLength(20);
                entity.Property(unitType => unitType.Name).IsRequired().HasMaxLength(50);
                entity.Property(unitType => unitType.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(unitType => unitType.Code).IsUnique();

                // Relationships
                entity
                    .HasMany(unitType => unitType.ProfitLossItems)
                    .WithOne(item => item.UnitType)
                    .HasForeignKey(item => item.UnitTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity
                    .HasMany(unitType => unitType.VendorOffers)
                    .WithOne(offer => offer.UnitType)
                    .HasForeignKey(offer => offer.UnitTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        #endregion

        #region DocumentApprovalRules

        private static void ConfigureDocumentApprovalRules(ModelBuilder builder)
        {
            builder.Entity<DocumentApprovalRule>(entity =>
            {
                entity.HasKey(r => r.DocumentApprovalRuleId);
                entity.Property(r => r.MinAmount).HasPrecision(18, 2);
                entity.Property(r => r.MaxAmount).HasPrecision(18, 2);
                entity.Property(r => r.Sequence).HasDefaultValue(1);
                entity.Property(r => r.IsActive).HasDefaultValue(true);
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity
                    .HasOne(r => r.DocumentType)
                    .WithMany()
                    .HasForeignKey(r => r.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasOne(r => r.JobType)
                    .WithMany()
                    .HasForeignKey(r => r.JobTypeId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(r => new
                {
                    r.DocumentTypeId,
                    r.JobTypeId,
                    r.ProcurementCategory,
                    r.MinAmount,
                    r.MaxAmount,
                    r.IsActive,
                });
            });
        }

        #endregion

        #region Notification

        private static void ConfigureNotification(ModelBuilder builder)
        {
            builder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.NotificationId);

                entity.Property(n => n.Title).IsRequired().HasMaxLength(200);

                entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);

                entity.Property(n => n.NotificationType).IsRequired().HasMaxLength(50);

                entity.Property(n => n.ActionUrl).HasMaxLength(500);

                entity.Property(n => n.ReferenceId).HasMaxLength(450);

                entity.Property(n => n.IsRead).HasDefaultValue(false);

                entity.Property(n => n.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Relationship with User (recipient)
                entity
                    .HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship with Creator (optional)
                entity
                    .HasOne(n => n.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(n => n.CreatedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes for common queries
                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => new { n.UserId, n.IsRead });
                entity.HasIndex(n => n.CreatedAt);
                entity.HasIndex(n => n.NotificationType);
            });
        }

        #endregion
    }
}
