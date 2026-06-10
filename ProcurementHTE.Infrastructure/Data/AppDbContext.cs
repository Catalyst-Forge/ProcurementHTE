using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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
        public DbSet<ProcurementStatusHistory> ProcurementStatusHistories { get; set; }
        public DbSet<JobTypes> JobTypes { get; set; }
        public DbSet<JobTypeDocuments> JobTypeDocuments { get; set; }
        public DbSet<ProcDetail> ProcDetails { get; set; }
        public DbSet<ProcOffer> ProcOffers { get; set; }
        public DbSet<ProcDocuments> ProcDocuments { get; set; }
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

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            UnitTypeSeeder.SeedUnitTypes(builder);
            ConfigureGlobalQueryFilters(builder);
        }

        private static void ConfigureGlobalQueryFilters(ModelBuilder builder)
        {
            builder.Entity<Procurement>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<ProfitLoss>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<VendorOffer>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<ProcDocuments>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<PurchaseRequisition>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<Vendor>().HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
