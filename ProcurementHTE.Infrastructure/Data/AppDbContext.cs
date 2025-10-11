using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<User, Role, string>(options)
    {
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<WoTypes> WoTypes { get; set; }
        public DbSet<WoDetail> WoDetails { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Tender> Tenders { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<DocumentApprovals> DocumentApprovals { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<VendorOffer> VendorOffers { get; set; }
        public DbSet<VendorWorkOrder> VendorWorkOrders { get; set; }
        public DbSet<WoDocumentApprovals> WoDocumentApprovals { get; set; }
        public DbSet<WoDocuments> WoDocuments { get; set; }
        public DbSet<WoTypeDocuments> WoTypesDocuments { get; set; }

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

            // Generate Id
            builder.Entity<WorkOrder>().Property(wo => wo.WorkOrderId).ValueGeneratedOnAdd();
            builder.Entity<Tender>().Property(t => t.TenderId).ValueGeneratedOnAdd();
            builder.Entity<Vendor>().Property(v => v.VendorId).ValueGeneratedOnAdd();

            // Enum to string
            builder.Entity<WorkOrder>().Property(p => p.ProcurementType).HasConversion<string>();

            // Relations
            builder
                .Entity<WorkOrder>()
                .HasOne(w => w.User)
                .WithMany(u => u.WorkOrders)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<WorkOrder>()
                .HasOne(o => o.WoType)
                .WithMany(wot => wot.WorkOrders)
                .HasForeignKey(o => o.WoTypeId)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<WorkOrder>()
                .HasMany(w => w.WoDocuments)
                .WithOne()
                .HasForeignKey(d => d.WoNum)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<WoDetail>()
                .HasOne(d => d.WorkOrder)
                .WithMany()
                .HasForeignKey(d => d.WoNum)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<VendorWorkOrder>()
                .HasOne(v => v.WorkOrder)
                .WithMany()
                .HasForeignKey(v => v.WoNum)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<VendorWorkOrder>()
                .HasOne(vwo => vwo.Vendor)
                .WithMany(v => v.VendorWorkOrders)
                .HasPrincipalKey(v => v.VendorCode)
                .HasForeignKey(v => v.VendorCode)
                .OnDelete(DeleteBehavior.NoAction);
            builder.Entity<VendorWorkOrder>().HasAlternateKey(vw => vw.WoNum);
            builder
                .Entity<VendorOffer>()
                .HasOne(o => o.VendorWorkOrder)
                .WithMany(vwo => vwo.VendorOffers)
                .HasForeignKey(o => o.WoNum)
                .HasPrincipalKey(vw => vw.WoNum)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<WoDocuments>()
                .HasOne(d => d.DocumentType)
                .WithMany(docType => docType.WoDocuments)
                .HasForeignKey(d => d.DocumentTypeId)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<WoTypeDocuments>()
                .HasOne(o => o.WoType)
                .WithMany(wot => wot.WoTypeDocuments)
                .HasForeignKey(o => o.WoTypeId)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<WoTypeDocuments>()
                .HasOne(o => o.DocumentType)
                .WithMany(docType => docType.WoTypeDocuments)
                .HasForeignKey(o => o.DocumentTypeId)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<DocumentApprovals>()
                .HasOne(a => a.Role)
                .WithMany()
                .HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<WoDocumentApprovals>()
                .HasOne(a => a.Role)
                .WithMany()
                .HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<WoDocumentApprovals>()
                .HasOne(a => a.WoDocument)
                .WithMany(wod => wod.WoDocumentApprovals)
                .HasForeignKey(a => a.WoDocumentId)
                .OnDelete(DeleteBehavior.NoAction);
            builder
                .Entity<WoDocumentApprovals>()
                .HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApproverId)
                .OnDelete(DeleteBehavior.NoAction);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}
