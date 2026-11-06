using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Tests.Services;

public class NotificationReadStateTests
{
    [Theory]
    [InlineData("owner", true)]
    [InlineData("someone-else", false)]
    public async Task MarkAsRead_OnlyWorksForTheOwner(string userId, bool expectedRead)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var notification = new Notification
        {
            UserId = "owner",
            Title = "Title",
            Message = "Message",
            NotificationType = "Test",
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        await new NotificationRepository(db).MarkAsReadAsync(notification.NotificationId, userId);

        Assert.Equal(expectedRead, notification.IsRead);
    }
}
