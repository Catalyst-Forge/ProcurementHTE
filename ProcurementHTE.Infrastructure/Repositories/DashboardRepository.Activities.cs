using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class DashboardRepository
    {
        public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int take = 10)
        {
            var procurements = await _context
                .Procurements.AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .Select(p => new RecentActivityDto
                {
                    Time = p.CreatedAt,
                    User = p.User != null ? p.User.FullName : "Unknown",
                    Action = $"Created Procurement {p.ProcNum}",
                    Description = p.JobName ?? p.Note,
                })
                .ToListAsync();

            var docs = await _context
                .ProcDocuments.AsNoTracking()
                .OrderByDescending(doc => doc.CreatedAt)
                .Take(50)
                .Select(doc => new RecentActivityDto
                {
                    Time = doc.CreatedAt,
                    User = doc.Procurement.User != null ? doc.Procurement.User.FullName : "Unknown",
                    Action = $"Uploaded Document {doc.FileName}",
                    Description = $"For Procurement {doc.Procurement!.ProcNum} Upload Document",
                })
                .ToListAsync();

            var pnl = await _context
                .ProfitLosses.AsNoTracking()
                .OrderByDescending(pnl => pnl.CreatedAt)
                .Take(50)
                .Select(pnl => new RecentActivityDto
                {
                    Time = pnl.CreatedAt,
                    User = pnl.Procurement.User != null ? pnl.Procurement.User.FullName : "Unknown",
                    Action = "Created Profit & Loss Record",
                    Description =
                        $"For Procurement {pnl.Procurement!.ProcNum} Create Profit & Loss Record",
                })
                .ToListAsync();

            return procurements
                .Concat(docs)
                .Concat(pnl)
                .OrderByDescending(activity => activity.Time)
                .Take(take)
                .ToList();
        }

        public async Task<List<RecentLoginSummary>> GetUserActivityStatusAsync(
            int take = 30,
            CancellationToken ct = default
        )
        {
            var onlineThreshold = DateTime.Now.AddMinutes(-15);

            return await _context
                .Users.Where(u => u.LastLoginAt != null && u.IsActive)
                .OrderByDescending(u => u.LastLoginAt)
                .Take(take)
                .Select(u => new RecentLoginSummary
                {
                    UserId = u.Id,
                    FullName = u.FullName ?? string.Empty,
                    UserName = u.UserName ?? string.Empty,
                    JobTitle = u.JobTitle,
                    LastLoginAt = u.LastLoginAt!.Value,
                    IsOnline = u.LastLoginAt >= onlineThreshold,
                })
                .ToListAsync(ct);
        }
    }
}
