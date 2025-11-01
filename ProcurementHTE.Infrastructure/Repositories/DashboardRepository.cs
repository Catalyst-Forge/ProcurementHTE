using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int take = 10)
        {
            var wo = await _context
                .WorkOrders.AsNoTracking()
                .OrderByDescending(wo => wo.CreatedAt)
                .Take(50)
                .Select(wo => new RecentActivityDto
                {
                    Time = wo.CreatedAt,
                    User = wo.User != null ? wo.User.FullName : "Unknown",
                    Action = $"Created Work Order {wo.WoNum}",
                    Description = wo.Description,
                })
                .ToListAsync();

            var docs = await _context
                .WoDocuments.AsNoTracking()
                .OrderByDescending(doc => doc.CreatedAt)
                .Take(50)
                .Select(doc => new RecentActivityDto
                {
                    Time = doc.CreatedAt,
                    User = doc.WorkOrder.User != null ? doc.WorkOrder.User.FullName : "Unknown",
                    Action = $"Uploaded Document {doc.FileName}",
                    Description = $"For Work Order {doc.WorkOrder!.WoNum}" + " Upload Document",
                })
                .ToListAsync();

            var pnl = await _context
                .ProfitLosses.AsNoTracking()
                .OrderByDescending(pnl => pnl.CreatedAt)
                .Take(50)
                .Select(pnl => new RecentActivityDto
                {
                    Time = pnl.CreatedAt,
                    User = pnl.WorkOrder.User != null ? pnl.WorkOrder.User.FullName : "Unknown",
                    Action = $"Created Profit & Loss Record",
                    Description =
                        $"For Work Order {pnl.WorkOrder!.WoNum}" + " Create Profit & Loss Record",
                })
                .ToListAsync();

            return wo.Concat(docs)
                .Concat(pnl)
                .OrderByDescending(activity => activity.Time)
                .Take(take)
                .ToList();
        }

        public async Task<IReadOnlyList<ApprovalStatusCountDto>> GetApprovalStatusCountsAsync()
        {
            return await _context
                .WoDocumentApprovals.Where(a => a.Status == "Pending").GroupBy(d => d.Status)
                .Select(g => new ApprovalStatusCountDto { Status = g.Key, Count = g.Count() })
                .ToListAsync();
        }
    }
}
