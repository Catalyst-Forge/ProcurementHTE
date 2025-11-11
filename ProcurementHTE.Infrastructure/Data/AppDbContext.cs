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
        public DbSet<ProfitLossSelectedVendor> ProfitLossSelectedVendors { get; set; }
        public DbSet<DocumentApprovals> DocumentApprovals { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<WoDocuments> WoDocuments { get; set; }
        public DbSet<WoTypeDocuments> WoTypesDocuments { get; set; }
        public DbSet<WoDocumentApprovals> WoDocumentApprovals { get; set; }
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
                .WithOne(woDocument => woDocument.WorkOrder)
                .HasForeignKey(woDocument => woDocument.WorkOrderId)
                .OnDelete(DeleteBehavior.NoAction); // Relation to WoDocument
            builder
                .Entity<WorkOrder>()
                .HasMany(workOrder => workOrder.VendorOffers)
                .WithOne(vendorOffer => vendorOffer.WorkOrder)
                .HasForeignKey(vendorOffer => vendorOffer.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade); // Relation to Vendor Offer

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
            builder.Entity<WoDocuments>(entity =>
            {
                // relasi ke DocumentType (sudah ada di kode lama kamu—aku rapikan property names)
                entity
                    .HasOne(d => d.DocumentType)
                    .WithMany(t => t.WoDocuments)
                    .HasForeignKey(d => d.DocumentTypeId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(d => d.QrText).HasDatabaseName("IX_WoDocuments_QrText");
                entity
                    .HasIndex(d => new { d.WorkOrderId, d.CreatedAt })
                    .HasDatabaseName("IX_WoDocuments_WorkOrderId_CreatedAt");

                // relasi ke WorkOrder (kamu sudah mengatur di WorkOrder.WithMany(WoDocuments))
                // indeks & constraints untuk MinIO
                entity.Property(d => d.FileName).HasMaxLength(300).IsRequired();
                entity.Property(d => d.ObjectKey).HasMaxLength(600).IsRequired();
                entity.Property(d => d.ContentType).HasMaxLength(150).IsRequired();
                entity.Property(d => d.Status).HasMaxLength(16).HasDefaultValue("Uploaded");
                entity.Property(d => d.Description).HasMaxLength(200).IsRequired(false);

                // satu dokumen "status" per (WO, DocumentType) (misal menjaga 1 'Uploaded' aktif per type)
                entity
                    .HasIndex(d => new
                    {
                        d.WorkOrderId,
                        d.DocumentTypeId,
                        d.Status,
                    })
                    .IsUnique();
            });

            // ***** WO Document Approval *****
            builder.Entity<WoDocumentApprovals>(entity =>
            {
                entity.Property(a => a.Status).HasMaxLength(16).HasDefaultValue("Pending");

                // FK ke WorkOrders (INI WAJIB, sebelumnya belum ada di kode kamu)
                entity
                    .HasOne(a => a.WorkOrder)
                    .WithMany( /* w => w.WoDocumentApprovals */
                    ) // boleh tanpa nav kalau tidak kamu tambahkan di WorkOrder
                    .HasForeignKey(a => a.WorkOrderId)
                    .OnDelete(DeleteBehavior.NoAction);

                // FK ke WoDocuments (perbaiki WithMany: gunakan koleksi Approvals di WoDocuments)
                entity
                    .HasOne(a => a.WoDocument)
                    .WithMany(d => d.Approvals)
                    .HasForeignKey(a => a.WoDocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity
                    .HasOne(a => a.Role)
                    .WithMany()
                    .HasForeignKey(a => a.RoleId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasOne(a => a.Approver)
                    .WithMany()
                    .HasForeignKey(a => a.ApproverId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity
                    .HasIndex(a => new
                    {
                        a.WoDocumentId,
                        a.Level,
                        a.SequenceOrder,
                    })
                    .IsUnique()
                    .HasDatabaseName("UX_WoDocApprovals_Doc_Level_Seq");

                // 2) Index untuk inbox approver (Role + Status)
                // IncludeProperties akan dibuat sebagai INCLUDE index (SQL Server)
                entity
                    .HasIndex(a => new { a.RoleId, a.Status })
                    .HasDatabaseName("IX_WoDocApprovals_Role_Status")
                    .IncludeProperties(a => new
                    {
                        a.WoDocumentId,
                        a.WorkOrderId,
                        a.Level,
                        a.SequenceOrder,
                    });
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}
