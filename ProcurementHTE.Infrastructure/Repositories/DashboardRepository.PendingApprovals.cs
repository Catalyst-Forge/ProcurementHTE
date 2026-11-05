using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class DashboardRepository
    {
        public async Task<int> GetPendingApprovalCountByUserAsync(
            string userId,
            string[] userRoles,
            CancellationToken ct = default
        )
        {
            var isAdmin = userRoles.Contains("Admin");
            var isAnalyst = userRoles.Contains("Analyst HTE & LTS");
            var isAsstManager = userRoles.Contains("Assistant Manager HTE");
            var isManager = userRoles.Contains("Manager Transport & Logistic");
            var isVicePresident = userRoles.Contains("Vice President");
            var query = _context.Procurements.AsQueryable();

            if (isAdmin)
            {
                query = query.Where(p =>
                    p.ProcurementStatus == ProcurementStatus.WaitingApprovalAnalyst
                    || p.ProcurementStatus == ProcurementStatus.WaitingApprovalAsstManager
                    || p.ProcurementStatus == ProcurementStatus.WaitingApprovalManager
                    || p.ProcurementStatus == ProcurementStatus.WaitingApprovalVP
                );
            }
            else
            {
                query = query.Where(p =>
                    (
                        isAnalyst
                        && p.ProcurementStatus == ProcurementStatus.WaitingApprovalAnalyst
                        && p.AnalystHteUserId == userId
                    )
                    || (
                        isAsstManager
                        && p.ProcurementStatus == ProcurementStatus.WaitingApprovalAsstManager
                        && p.AssistantManagerUserId == userId
                    )
                    || (
                        isManager
                        && p.ProcurementStatus == ProcurementStatus.WaitingApprovalManager
                        && p.ManagerUserId == userId
                    )
                    || (
                        isVicePresident
                        && p.ProcurementStatus == ProcurementStatus.WaitingApprovalVP
                        && p.VicePresidentUserId == userId
                    )
                );
            }

            return await query.CountAsync(ct);
        }

        public async Task<(
            List<PendingApprovalItem> Items,
            int TotalCount
        )> GetPendingApprovalsByUserAsync(
            string userId,
            string[] userRoles,
            int skip = 0,
            int take = 15,
            CancellationToken ct = default
        )
        {
            var isAdmin = userRoles.Contains("Admin");
            var isAnalyst = userRoles.Contains("Analyst HTE & LTS");
            var isAsstManager = userRoles.Contains("Assistant Manager HTE");
            var isManager = userRoles.Contains("Manager Transport & Logistic");
            var isVicePresident = userRoles.Contains("Vice President");
            var seesEveryApproval = isAdmin || !(isAnalyst || isAsstManager || isManager || isVicePresident);
            var query = _context.Procurements.AsQueryable();

            if (seesEveryApproval)
            {
                query = query.Where(p =>
                    p.ProcurementStatus == ProcurementStatus.WaitingApprovalAnalyst
                    || p.ProcurementStatus == ProcurementStatus.WaitingApprovalAsstManager
                    || p.ProcurementStatus == ProcurementStatus.WaitingApprovalManager
                    || p.ProcurementStatus == ProcurementStatus.WaitingApprovalVP
                );
            }
            else
            {
                query = query.Where(p =>
                    (
                        isAnalyst
                        && p.ProcurementStatus == ProcurementStatus.WaitingApprovalAnalyst
                        && p.AnalystHteUserId == userId
                    )
                    || (
                        isAsstManager
                        && p.ProcurementStatus == ProcurementStatus.WaitingApprovalAsstManager
                        && p.AssistantManagerUserId == userId
                    )
                    || (
                        isManager
                        && p.ProcurementStatus == ProcurementStatus.WaitingApprovalManager
                        && p.ManagerUserId == userId
                    )
                    || (
                        isVicePresident
                        && p.ProcurementStatus == ProcurementStatus.WaitingApprovalVP
                        && p.VicePresidentUserId == userId
                    )
                );
            }

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(p => p.ApprovalTokenGeneratedAt ?? p.UpdatedAt ?? p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(p => new PendingApprovalItem(
                    p.ProcurementId,
                    p.ProcNum ?? "-",
                    p.Wonum ?? "-",
                    p.JobName ?? "-",
                    p.ProcurementStatus.ToString(),
                    GetStatusDescription(p.ProcurementStatus),
                    p.DocumentDate,
                    p.ApprovalTokenGeneratedAt
                ))
                .ToListAsync(ct);

            return (items, totalCount);
        }
    }
}
