using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IWorkOrderService _woService;

        public DashboardController(IWorkOrderService woService)
        {
            _woService = woService;
        }

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }
            var recentWo = await _woService.GetMyRecentWorkOrderAsync(userId, 5, ct);
            ViewBag.TotalWo = await _woService.CountAllWoAsync(ct);
            var woViewModel = recentWo.Select(item => new DashboardWoViewModel
            {
                WoNum = item.WoNum,
                Description = item.Description,
                ProcurementType = item.ProcurementType,
                StatusName = item.Status?.StatusName,
                CreatedAt = item.CreatedAt,
            });
            return View(woViewModel);
        }
    }
}
