using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity
            .Property(u => u.FullName)
            .HasComputedColumnSql("CONCAT([FirstName], ' ', [LastName])");

        entity
            .Property(u => u.TwoFactorMethod)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(TwoFactorMethod.None);

        entity
            .HasMany(u => u.Sessions)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasMany(u => u.SecurityLogs)
            .WithOne(log => log.User)
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> entity)
    {
        entity.HasKey(ur => new { ur.UserId, ur.RoleId });
    }
}

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> entity)
    {
        entity.HasKey(session => session.UserSessionId);
        entity.Property(session => session.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        entity.Property(session => session.LastAccessedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}

public sealed class UserSecurityLogConfiguration : IEntityTypeConfiguration<UserSecurityLog>
{
    public void Configure(EntityTypeBuilder<UserSecurityLog> entity)
    {
        entity.HasKey(log => log.UserSecurityLogId);
        entity.Property(log => log.EventType).HasConversion<string>().HasMaxLength(50);
        entity.Property(log => log.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}
