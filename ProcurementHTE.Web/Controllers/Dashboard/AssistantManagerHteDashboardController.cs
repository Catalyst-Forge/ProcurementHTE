using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Authorization;

namespace ProcurementHTE.Web.Controllers.Dashboard
{
    [Authorize(Roles = DashboardRoleHelper.AssistantManagerHteRole)]
    [Route("Dashboard/AssistantManagerHte")]
    public class AssistantManagerHteDashboardController : DashboardBaseController
    {
        public AssistantManagerHteDashboardController(
            IProcurementQueryService procurementQueryService,
            UserManager<User> userManager,
            IProfitLossQueryService profitLossService,
            IDashboardService dashboardService
        )
            : base(procurementQueryService, userManager, profitLossService, dashboardService) { }

        [HttpGet("")]
        public Task<IActionResult> Index(CancellationToken ct = default) =>
            RenderDashboardAsync(
                "~/Views/Dashboard/AssistantManagerHte.cshtml",
                DashboardRoleHelper.AssistantManagerHteRole,
                ct
            );
    }
}
