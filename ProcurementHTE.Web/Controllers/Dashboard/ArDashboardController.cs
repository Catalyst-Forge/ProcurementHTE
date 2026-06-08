using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Authorization;

namespace ProcurementHTE.Web.Controllers.Dashboard
{
    [Authorize(Roles = "AR")]
    [Route("Dashboard/AR")]
    public class ArDashboardController : DashboardBaseController
    {
        public ArDashboardController(
            IProcurementQueryService procurementQueryService,
            UserManager<User> userManager,
            IProfitLossService profitLossService,
            IDashboardService dashboardService
        )
            : base(procurementQueryService, userManager, profitLossService, dashboardService) { }

        [HttpGet("")]
        public Task<IActionResult> Index(CancellationToken ct = default) =>
            RenderDashboardAsync(
                "~/Views/Dashboard/AR.cshtml",
                "AR",
                ct
            );
    }
}
