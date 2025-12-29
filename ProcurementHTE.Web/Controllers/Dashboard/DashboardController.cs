using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Web.Authorization;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.Dashboard
{
    [Authorize]
    [Route("Dashboard")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("/Dashboard")]
        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                TotalProcurements = await _context.Procurements.CountAsync(),
                ActiveProcurements = await _context
                    .Procurements.Where(p => p.Status != null)
                    .CountAsync(p =>
                        p.Status!.StatusName == "Created" || p.Status!.StatusName == "In Progress"
                    ),
                PendingApprovals = await _context.ProcDocumentApprovals.CountAsync(a =>
                    a.Status == "Pending"
                ),
                TotalVendors = await _context.Vendors.CountAsync(),

                TotalRevenue =
                    await _context.ProfitLossItems.SumAsync(pnl => (decimal?)pnl.Revenue) ?? 0m,
                TotalCost =
                    await _context.ProfitLosses.SumAsync(pnl =>
                        (decimal?)pnl.SelectedVendorFinalOffer
                    ) ?? 0m,
                TotalProfit =
                    await _context.ProfitLosses.SumAsync(pnl => (decimal?)pnl.Profit) ?? 0m,

                ProcurementsByStatus = await _context
                    .Procurements.Where(proc => proc.Status != null)
                    .GroupBy(proc => proc.Status!.StatusName)
                    .Select(g => new StatusCount { StatusName = g.Key, Count = g.Count() })
                    .ToListAsync(),

                MonthlyProcurementTrend = await GetMonthlyProcurementTrend(),

                RecentProcurements = await _context
                    .Procurements.Include(proc => proc.Status)
                    .Include(proc => proc.JobType)
                    .Include(proc => proc.User)
                    .Include(proc => proc.ProfitLosses)
                    .ThenInclude(pl => pl.Items)
                    .OrderByDescending(proc => proc.CreatedAt)
                    .Take(10)
                    .Select(proc => new ProcurementSummary
                    {
                        ProcNum = proc.ProcNum,
                        JobTypeName = proc.JobType != null ? proc.JobType.TypeName : string.Empty,
                        StatusName = proc.Status != null ? proc.Status.StatusName : string.Empty,
                        CreatedBy = proc.User != null ? proc.User.FullName! : string.Empty,
                        CreatedDate = proc.CreatedAt,
                        TotalAmount =
                            proc.ProfitLosses.SelectMany(pl => pl.Items)
                                .Sum(item => (decimal?)item.Revenue)
                            ?? 0m,
                    })
                    .ToListAsync(),

                PendingApprovalsDetail = await _context
                    .ProcDocumentApprovals.Include(a => a.ProcDocument)
                    .ThenInclude(d => d.Procurement)
                    .Include(a => a.Role)
                    .Where(a => a.Status == "Pending")
                    .OrderByDescending(a => a.ProcDocument.CreatedAt)
                    .Take(10)
                    .Select(a => new ApprovalSummary
                    {
                        ProcNum = a.ProcDocument.Procurement.ProcNum,
                        DocumentName = a.ProcDocument.FileName,
                        ApprovalRole = a.Role != null ? a.Role.Name! : string.Empty,
                        CreatedDate = a.ProcDocument.CreatedAt,
                    })
                    .ToListAsync(),

                JobTypeDistribution = await _context
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
                    .ToListAsync(),

                TopVendors = await _context
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
                    .Take(10)
                    .ToListAsync(),

                DocumentApprovalStats = await _context
                    .ProcDocumentApprovals.GroupBy(a => a.Status)
                    .Select(g => new StatusCount { StatusName = g.Key, Count = g.Count() })
                    .ToListAsync(),

                // Purchase Requisition Data
                TotalPurchaseRequisitions = await _context.PurchaseRequisitions.CountAsync(),

                RecentPurchaseRequisitions = await _context
                    .PurchaseRequisitions.Include(pr => pr.CreatedByUser)
                    .Include(pr => pr.Procurements)
                    .OrderByDescending(pr => pr.CreatedAt)
                    .Take(10)
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
                    .ToListAsync(),
            };

            return View(viewModel);
        }

        private async Task<List<MonthlyTrend>> GetMonthlyProcurementTrend()
        {
            var twelveMonthsAgo = DateTime.Now.AddMonths(-12);

            var monthlyData = await _context
                .Procurements.Include(p => p.ProfitLosses)
                .ThenInclude(pl => pl.Items)
                .Where(p => p.CreatedAt >= twelveMonthsAgo)
                .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                .Select(g => new MonthlyTrend
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    TotalValue = g.Sum(p =>
                        p.ProfitLosses.SelectMany(pl => pl.Items)
                            .Sum(item => (decimal?)item.Revenue)
                        ?? 0m
                    ),
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            return monthlyData;
        }

        [HttpGet]
        public async Task<IActionResult> GetProcurementStatusChart()
        {
            var data = await _context
                .Procurements.Where(p => p.Status != null)
                .GroupBy(p => p.Status!.StatusName)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyTrendChart()
        {
            var data = await GetMonthlyProcurementTrend();
            return Json(data);
        }
    }
}
