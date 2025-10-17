using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<User, Role, string>(options)
    {
        public DbSet<Status> Statuses { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<Tender> Tenders { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<WoTypes> WoTypes { get; set; }
        public DbSet<WoDetail> WoDetails { get; set; }
        public DbSet<ProfitLoss> ProfitLosses { get; set; }
        public DbSet<VendorOffer> VendorOffers { get; set; }
        public DbSet<DocumentApprovals> DocumentApprovals { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<WoDocuments> WoDocuments { get; set; }
        public DbSet<WoTypeDocuments> WoTypesDocuments { get; set; }
        public DbSet<WoDocumentApprovals> WoDocumentApprovals { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Concat Fullname for User
            builder
                .Entity<User>()
                .Property(u => u.FullName)
                .HasComputedColumnSql("CONCAT([FirstName], ' ', [LastName])");

            // Composite Keys
            builder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });

            builder.Entity<WorkOrder>(entity =>
            {
                entity.HasKey(x => x.WorkOrderId);
                entity.Property(x => x.WoNum).IsRequired();
                entity
                    .HasIndex(w => new { w.UserId, w.CreatedAt })
                    .HasDatabaseName("IX_WorkOrders_UserId_CreatedAt")
                    .IsDescending(false, true);

                entity
                    .HasIndex(w => new { w.UserId, w.CreatedAt })
                    .HasDatabaseName("IX_WorkOrders_UserId_CreatedAt_Covering")
                    .IncludeProperties(w => new
                    {
                        w.WoNum,
                        w.Description,
                        w.StatusId,
                    });
                entity.HasAlternateKey(w => w.WoNum).HasName("AK_WorkOrders_WoNum");
            });

            builder.Entity<Vendor>(entity =>
            {
                entity.HasKey(x => x.VendorId);
                entity.Property(x => x.VendorCode).IsRequired();
            });

            // Generate Id
            builder.Entity<WorkOrder>().Property(wo => wo.WorkOrderId).ValueGeneratedOnAdd();
            builder.Entity<Tender>().Property(t => t.TenderId).ValueGeneratedOnAdd();
            builder.Entity<Vendor>().Property(v => v.VendorId).ValueGeneratedOnAdd();
            builder
                .Entity<DocumentApprovals>()
                .Property(da => da.DocumentApprovalId)
                .ValueGeneratedOnAdd();
            builder.Entity<DocumentType>().Property(dt => dt.DocumentTypeId).ValueGeneratedOnAdd();
            builder.Entity<ProfitLoss>().Property(pnl => pnl.ProfitLossId).ValueGeneratedOnAdd();
            builder.Entity<VendorOffer>().Property(vo => vo.VendorOfferId).ValueGeneratedOnAdd();
            builder.Entity<WoDetail>().Property(wod => wod.WoDetailId).ValueGeneratedOnAdd();
            builder
                .Entity<WoDocumentApprovals>()
                .Property(woda => woda.WoDocumentApprovalId)
                .ValueGeneratedOnAdd();
            builder
                .Entity<WoDocuments>()
                .Property(wodoc => wodoc.WoDocumentId)
                .ValueGeneratedOnAdd();
            builder
                .Entity<WoTypeDocuments>()
                .Property(wtd => wtd.WoTypeDocumentId)
                .ValueGeneratedOnAdd();
            builder.Entity<WoTypes>().Property(wot => wot.WoTypeId).ValueGeneratedOnAdd();

            // Enum to string
            builder.Entity<WorkOrder>().Property(p => p.ProcurementType).HasConversion<string>();

            // Unique
            builder.Entity<Vendor>().HasIndex(v => v.VendorCode).IsUnique();

            // Relations
            // ***** Work Order *****
            builder
                .Entity<WorkOrder>()
                .HasOne(workOrder => workOrder.User)
                .WithMany(user => user.WorkOrders)
                .HasForeignKey(workOrder => workOrder.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to User
            builder
                .Entity<WorkOrder>()
                .HasOne(workOrder => workOrder.WoType)
                .WithMany(woType => woType.WorkOrders)
                .HasForeignKey(workOrder => workOrder.WoTypeId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to WoType
            builder
                .Entity<WorkOrder>()
                .HasMany(workOrder => workOrder.WoDocuments)
                .WithOne()
                .HasForeignKey(woDocument => woDocument.WorkOrderId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to WoDocument
            builder
                .Entity<WorkOrder>()
                .HasMany(workOrder => workOrder.VendorOffers)
                .WithOne(vendorOffer => vendorOffer.WorkOrder)
                .HasForeignKey(vendorOffer => vendorOffer.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade); // Relation to Vendor Offer
            builder
                .Entity<WorkOrder>()
                .HasOne(workOrder => workOrder.Vendor)
                .WithMany()
                .HasForeignKey(workOrder => workOrder.VendorId)
                .OnDelete(DeleteBehavior.SetNull);

            // ***** WO Detail *****
            builder
                .Entity<WoDetail>()
                .HasOne(woDetail => woDetail.WorkOrder)
                .WithMany(workOrder => workOrder.WoDetails)
                .HasForeignKey(woDetail => woDetail.WorkOrderId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to WorkOrder

            // ***** Vendor Offer *****
            builder
                .Entity<VendorOffer>()
                .HasOne(vendorOffer => vendorOffer.WorkOrder)
                .WithMany(workOrder => workOrder.VendorOffers)
                .HasForeignKey(vendorOffer => vendorOffer.WorkOrderId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to WorkOrder

            // ***** WO Document *****
            builder
                .Entity<WoDocuments>()
                .HasOne(WoDocument => WoDocument.DocumentType)
                .WithMany(documentType => documentType.WoDocuments)
                .HasForeignKey(documentType => documentType.DocumentTypeId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to Document Type

            // ***** WO Type Document *****
            builder
                .Entity<WoTypeDocuments>()
                .HasOne(woTypeDocument => woTypeDocument.WoType)
                .WithMany(woType => woType.WoTypeDocuments)
                .HasForeignKey(woTypeDocument => woTypeDocument.WoTypeId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to WO Type
            builder
                .Entity<WoTypeDocuments>()
                .HasOne(woTypeDocument => woTypeDocument.DocumentType)
                .WithMany(documentType => documentType.WoTypeDocuments)
                .HasForeignKey(woTypeDocument => woTypeDocument.DocumentTypeId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to Document Type

            // ***** Document Approval *****
            builder
                .Entity<DocumentApprovals>()
                .HasOne(documentApproval => documentApproval.Role)
                .WithMany()
                .HasForeignKey(documentApproval => documentApproval.RoleId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to Role

            // ***** WO Document Approval *****
            builder
                .Entity<WoDocumentApprovals>()
                .HasOne(woDocumentApproval => woDocumentApproval.Role)
                .WithMany()
                .HasForeignKey(woDocumentApproval => woDocumentApproval.RoleId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to Role
            builder
                .Entity<WoDocumentApprovals>()
                .HasOne(woDocumentApproval => woDocumentApproval.WoDocument)
                .WithMany(woDocument => woDocument.WoDocumentApprovals)
                .HasForeignKey(woDocumentApproval => woDocumentApproval.WoDocumentId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to WO Document
            builder
                .Entity<WoDocumentApprovals>()
                .HasOne(woDocumentApproval => woDocumentApproval.Approver)
                .WithMany()
                .HasForeignKey(woDocumentApproval => woDocumentApproval.ApproverId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to User

            // ***** Profit and Loss *****
            builder
                .Entity<ProfitLoss>()
                .HasOne(profitLoss => profitLoss.SelectedVendorOffer)
                .WithMany()
                .HasForeignKey(profitLoss => profitLoss.SelectedVendorOfferId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}
