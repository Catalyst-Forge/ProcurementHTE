using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Web.Hubs
{
    [Authorize]
    public class DashboardHub : Hub, IDashboardHub
    {
        // Static in-memory dictionary for tracking online users (no database updates needed)
        private static readonly ConcurrentDictionary<string, UserConnectionInfo> OnlineUsers =
            new();

        private readonly IUserActivityNotifier _userActivityNotifier;

        public DashboardHub(IUserActivityNotifier userActivityNotifier)
        {
            _userActivityNotifier = userActivityNotifier;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var username = Context.User?.Identity?.Name ?? "Anonymous";

            // Join user-specific group
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                // Add or update user in in-memory tracking
                var connectionInfo = OnlineUsers.AddOrUpdate(
                    userId,
                    new UserConnectionInfo
                    {
                        UserId = userId,
                        UserName = username,
                        ConnectionId = Context.ConnectionId,
                        ConnectedAt = DateTime.Now,
                        LastActivityAt = DateTime.Now,
                    },
                    (key, existing) =>
                    {
                        existing.ConnectionId = Context.ConnectionId;
                        existing.UserName = username;
                        existing.LastActivityAt = DateTime.Now;
                        return existing;
                    }
                );

                // Notify all admins about user coming online
                await _userActivityNotifier.NotifyUserActivityAsync(
                    userId,
                    username,
                    isOnline: true
                );
            }

            // Join role-based groups
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var username = Context.User?.Identity?.Name ?? "Unknown";

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

                // Remove user from in-memory tracking
                OnlineUsers.TryRemove(userId, out _);

                // Notify all admins about user going offline
                await _userActivityNotifier.NotifyUserActivityAsync(
                    userId,
                    username,
                    isOnline: false
                );
            }

            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Client calls this periodically to update last activity timestamp
        public Task UpdateActivity()
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId) && OnlineUsers.TryGetValue(userId, out var userInfo))
            {
                userInfo.LastActivityAt = DateTime.Now;
            }

            return Task.CompletedTask;
        }

        // Client explicitly calls this before logout/disconnect
        public async Task NotifyLogout()
        {
            var userId = Context.UserIdentifier;
            var username = Context.User?.Identity?.Name ?? "Unknown";

            if (!string.IsNullOrEmpty(userId))
            {
                // Remove from online users immediately
                OnlineUsers.TryRemove(userId, out _);

                // Broadcast offline status to all clients
                await _userActivityNotifier.NotifyUserActivityAsync(
                    userId,
                    username,
                    isOnline: false
                );
            }
        }

        // Client can call this to request latest dashboard data
        public async Task RequestDashboardUpdate()
        {
            await Clients.Caller.SendAsync("DashboardUpdateRequested");
        }
        
        // Client calls this when entering Dashboard page to receive UserActivityChanged events
        public async Task JoinDashboardViewers()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard_viewers");
        }
        
        // Client calls this when leaving Dashboard page
        public async Task LeaveDashboardViewers()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard_viewers");
        }

        // Static method to check if specific user is online (called from repository)
        public static bool IsUserOnline(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            if (OnlineUsers.TryGetValue(userId, out var userInfo))
            {
                // Consider user online if last activity was within 5 minutes
                var timeSinceActivity = DateTime.Now - userInfo.LastActivityAt;
                return timeSinceActivity.TotalMinutes <= 5;
            }

            return false;
        }

        // Static method to get all currently online users
        public static IReadOnlyList<UserConnectionInfo> GetOnlineUsers()
        {
            var onlineThreshold = DateTime.Now.AddMinutes(-5);
            return OnlineUsers
                .Values.Where(u => u.LastActivityAt > onlineThreshold)
                .ToList()
                .AsReadOnly();
        }

        // Static method to get count of online users
        public static int GetOnlineUsersCount()
        {
            var onlineThreshold = DateTime.Now.AddMinutes(-5);
            return OnlineUsers.Values.Count(u => u.LastActivityAt > onlineThreshold);
        }
    }

    // Connection info class for in-memory tracking
    public class UserConnectionInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
    }
}
