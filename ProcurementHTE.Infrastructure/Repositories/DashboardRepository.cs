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
                    Description = $"For Procurement {pnl.Procurement!.ProcNum} Create Profit & Loss Record",
                })
                .ToListAsync();

            return procurements.Concat(docs)
                .Concat(pnl)
                .OrderByDescending(activity => activity.Time)
                .Take(take)
                .ToList();
        }

        public async Task<IReadOnlyList<ApprovalStatusCountDto>> GetApprovalStatusCountsAsync()
        {
            return await _context
                .ProcDocumentApprovals.Where(a => a.Status == "Pending").GroupBy(d => d.Status)
                .Select(g => new ApprovalStatusCountDto { Status = g.Key, Count = g.Count() })
                .ToListAsync();
        }
    }
}
