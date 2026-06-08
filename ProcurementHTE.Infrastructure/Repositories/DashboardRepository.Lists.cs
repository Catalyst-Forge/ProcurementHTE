using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class DashboardRepository
    {
        public async Task<List<ProcurementSummary>> GetRecentProcurementsAsync(
            int take = 10,
            CancellationToken ct = default
        )
        {
            return await _context
                .Procurements.Include(proc => proc.Status)
                .Include(proc => proc.JobType)
                .Include(proc => proc.User)
                .Include(proc => proc.ProfitLosses)
                .ThenInclude(pl => pl.Items)
                .OrderByDescending(proc => proc.CreatedAt)
                .Take(take)
                .Select(proc => new ProcurementSummary
                {
                    ProcNum = proc.ProcNum ?? string.Empty,
                    JobTypeName = proc.JobType != null ? proc.JobType.TypeName : string.Empty,
                    StatusName = proc.Status != null ? proc.Status.StatusName : string.Empty,
                    CreatedBy =
                        proc.User != null ? proc.User.FullName ?? string.Empty : string.Empty,
                    CreatedDate = proc.CreatedAt,
                    TotalAmount =
                        proc.ProfitLosses.SelectMany(pl => pl.Items)
                            .Sum(item => (decimal?)item.Revenue)
                        ?? 0m,
                })
                .ToListAsync(ct);
        }

        public async Task<List<ApprovalSummary>> GetPendingApprovalsDetailAsync(
            int take = 10,
            CancellationToken ct = default
        )
        {
            return await _context
                .PurchaseRequisitions.Include(pr => pr.Procurements)
                .Where(pr =>
                    pr.Status == PurchaseRequisitionStatus.WaitingApprovalAnalyst
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalAsstManager
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalManager
                )
                .OrderByDescending(pr => pr.CreatedAt)
                .Take(take)
                .Select(pr => new ApprovalSummary
                {
                    ProcNum =
                        pr.Procurements.FirstOrDefault() != null
                            ? pr.Procurements.First().ProcNum ?? "-"
                            : "-",
                    DocumentName = pr.PrNumber ?? "-",
                    ApprovalRole = pr.Status.ToString(),
                    CreatedDate = pr.CreatedAt,
                })
                .ToListAsync(ct);
        }

        public async Task<List<JobTypeCount>> GetJobTypeDistributionAsync(
            CancellationToken ct = default
        )
        {
            return await _context
                .Procurements.Include(proc => proc.ProfitLosses)
                .ThenInclude(pl => pl.Items)
                .Where(proc => proc.JobType != null)
                .GroupBy(proc => proc.JobType!.TypeName)
                .Select(g => new JobTypeCount
                {
                    JobTypeName = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(proc =>
                        proc.ProfitLosses.SelectMany(pl => pl.Items)
                            .Sum(item => (decimal?)item.Revenue)
                        ?? 0m
                    ),
                })
                .OrderByDescending(j => j.Count)
                .ToListAsync(ct);
        }

        public async Task<List<VendorPerformance>> GetTopVendorsAsync(
            int take = 10,
            CancellationToken ct = default
        )
        {
            return await _context
                .Vendors.Select(v => new VendorPerformance
                {
                    VendorCode = v.VendorCode,
                    VendorName = v.VendorName,
                    OfferCount = _context.VendorOffers.Count(vo => vo.VendorId == v.VendorId),
                    SelectedCount = _context.ProfitLosses.Count(pl =>
                        pl.SelectedVendorId == v.VendorId
                    ),
                })
                .Where(v => v.OfferCount > 0)
                .OrderByDescending(v => v.OfferCount)
                .Take(take)
                .ToListAsync(ct);
        }

        public async Task<List<PurchaseRequisitionSummary>> GetRecentPurchaseRequisitionsAsync(
            int take = 10,
            CancellationToken ct = default
        )
        {
            return await _context
                .PurchaseRequisitions.Include(pr => pr.CreatedByUser)
                .Include(pr => pr.Procurements)
                .OrderByDescending(pr => pr.CreatedAt)
                .Take(take)
                .Select(pr => new PurchaseRequisitionSummary
                {
                    PrId = pr.PrId,
                    PrNumber = pr.PrNumber,
                    RequestDate = pr.RequestDate,
                    Description = pr.Description,
                    CreatedBy =
                        pr.CreatedByUser != null
                            ? pr.CreatedByUser.FullName ?? string.Empty
                            : string.Empty,
                    CreatedAt = pr.CreatedAt,
                    ProcurementCount = pr.Procurements.Count,
                })
                .ToListAsync(ct);
        }
    }
}
