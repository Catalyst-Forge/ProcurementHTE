using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Authorization;

namespace ProcurementHTE.Web.Controllers.Dashboard
{
    [Authorize(Roles = DashboardRoleHelper.AppoRole)]
    [Route("Dashboard/Appo")]
    public class AppoDashboardController : DashboardBaseController
    {
        public AppoDashboardController(
            IProcurementService procurementService,
            UserManager<User> userManager,
            IProfitLossService profitLossService,
            IDashboardService dashboardService
        )
            : base(procurementService, userManager, profitLossService, dashboardService) { }

        [HttpGet("")]
        public Task<IActionResult> Index(CancellationToken ct = default) =>
            RenderDashboardAsync("~/Views/Dashboard/Appo.cshtml", DashboardRoleHelper.AppoRole, ct);
    }
}
