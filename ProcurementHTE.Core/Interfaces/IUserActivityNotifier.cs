namespace ProcurementHTE.Core.Interfaces
{
    /// <summary>
    /// Service for notifying about user activity changes (online/offline status)
    /// </summary>
    public interface IUserActivityNotifier
    {
        /// <summary>
        /// Notify all connected clients about user activity status change
        /// </summary>
        Task NotifyUserActivityAsync(string userId, string fullName, bool isOnline);

        /// <summary>
        /// Notify all connected clients to refresh dashboard data
        /// </summary>
        Task NotifyDashboardUpdateAsync();
    }
}
