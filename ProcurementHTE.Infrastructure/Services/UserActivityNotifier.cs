using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Infrastructure.Services
{
    /// <summary>
    /// Implementation of user activity notification service using SignalR.
    /// Note: This class depends on IHubContext which will be injected at runtime.
    /// The actual Hub type is resolved through DI registration.
    /// </summary>
    public class UserActivityNotifier<THub> : IUserActivityNotifier
        where THub : Hub
    {
        private readonly IHubContext<THub> _hubContext;
        private readonly ILogger<UserActivityNotifier<THub>> _logger;

        public UserActivityNotifier(
            IHubContext<THub> hubContext,
            ILogger<UserActivityNotifier<THub>> logger
        )
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyUserActivityAsync(string userId, string fullName, bool isOnline)
        {
            var status = isOnline ? "online" : "offline";

            _logger.LogInformation(
                "Broadcasting user activity: UserId={UserId}, FullName={FullName}, Status={Status}",
                userId,
                fullName,
                status
            );

            try
            {
                // Notify only clients in the "dashboard_viewers" group about user activity change
                // This prevents unnecessary broadcasts to pages that don't need this info
                await _hubContext.Clients.Group("dashboard_viewers").SendAsync(
                    "UserActivityChanged",
                    new
                    {
                        UserId = userId,
                        UserName = fullName,
                        FullName = fullName,
                        IsOnline = isOnline,
                        Status = status,
                        Timestamp = DateTime.Now,
                    }
                );

                _logger.LogInformation(
                    "Successfully broadcasted activity change for UserId={UserId}",
                    userId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to broadcast activity change for UserId={UserId}",
                    userId
                );
                throw;
            }
        }

        public async Task NotifyDashboardUpdateAsync()
        {
            try
            {
                _logger.LogInformation("Broadcasting dashboard update to all clients");
                await _hubContext.Clients.All.SendAsync("DashboardDataUpdated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast dashboard update");
                throw;
            }
        }
    }
}
