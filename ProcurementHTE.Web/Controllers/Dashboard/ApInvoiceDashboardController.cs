using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Authorization;

namespace ProcurementHTE.Web.Controllers.Dashboard
{
    [Authorize(Roles = "AP-Invoice")]
    [Route("Dashboard/APInvoice")]
    public class ApInvoiceDashboardController : DashboardBaseController
    {
        public ApInvoiceDashboardController(
            IProcurementQueryService procurementQueryService,
            UserManager<User> userManager,
            IProfitLossService profitLossService,
            IDashboardService dashboardService
        )
            : base(procurementQueryService, userManager, profitLossService, dashboardService) { }

        [HttpGet("")]
        public Task<IActionResult> Index(CancellationToken ct = default) =>
            RenderDashboardAsync(
                "~/Views/Dashboard/APInvoice.cshtml",
                "AP-Invoice",
                ct
            );
    }
}
