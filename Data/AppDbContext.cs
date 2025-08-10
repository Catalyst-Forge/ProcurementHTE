using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using project_25_07.Models;

namespace project_25_07.Data {
  public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<string>(options) {
    protected override void OnModelCreating(ModelBuilder builder) {
      base.OnModelCreating(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
      base.OnConfiguring(optionsBuilder);
    }
  }
}
