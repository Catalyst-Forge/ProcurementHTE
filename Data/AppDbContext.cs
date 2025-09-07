using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using project_25_07.Models;

namespace project_25_07.Data {
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
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
      base.OnConfiguring(optionsBuilder);
    }
  }
}
