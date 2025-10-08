using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data {
  public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User, Role, string>(options) {
    public DbSet<WorkOrder> WorkOrders { get; set; }
    public DbSet<WoTypes> WoTypes { get; set; }
    public DbSet<WoDetails> WoDetails { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<Tender> Tenders { get; set; }
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<ReasonRejected> ReasonRejecteds { get; set; }

    protected override void OnModelCreating(ModelBuilder builder) {
      base.OnModelCreating(builder);

      builder.Entity<User>().Property(u => u.FullName).HasComputedColumnSql("CONCAT([FirstName], ' ', [LastName])");

      builder.Entity<WorkOrder>().Property(wo => wo.WorkOrderId).ValueGeneratedOnAdd();
      builder.Entity<Tender>().Property(t => t.TenderId).ValueGeneratedOnAdd();
      builder.Entity<Vendor>().Property(v => v.VendorId).ValueGeneratedOnAdd();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
      base.OnConfiguring(optionsBuilder);
    }
  }
}
