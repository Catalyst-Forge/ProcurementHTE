using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> entity)
    {
        entity.HasKey(n => n.NotificationId);
        entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
        entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        entity.Property(n => n.NotificationType).IsRequired().HasMaxLength(50);
        entity.Property(n => n.ActionUrl).HasMaxLength(500);
        entity.Property(n => n.ReferenceId).HasMaxLength(450);
        entity.Property(n => n.IsRead).HasDefaultValue(false);
        entity.Property(n => n.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        entity.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(n => n.CreatedByUser).WithMany().HasForeignKey(n => n.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(n => n.UserId);
        entity.HasIndex(n => new { n.UserId, n.IsRead });
        entity.HasIndex(n => n.CreatedAt);
        entity.HasIndex(n => n.NotificationType);
    }
}
