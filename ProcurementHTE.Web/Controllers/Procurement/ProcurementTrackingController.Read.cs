using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementTrackingController
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("PendingApprovals")]
    public async Task<IActionResult> PendingApprovals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] bool partial = false,
        CancellationToken ct = default
    )
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(user);
        var skip = (page - 1) * pageSize;
        var (items, totalCount) = await _dashboardService.GetPendingApprovalsByUserAsync(
            userId,
            roles.ToArray(),
            skip,
            pageSize,
            ct
        );

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

        var isHtmxRequest = Request.Headers["HX-Request"].Count > 0;
        var isBoosted = Request.Headers["HX-Boosted"].Count > 0;
        if (partial || (isHtmxRequest && !isBoosted))
            return PartialView("_PendingApprovalsTable", items);

        return View(items);
    }

    [HttpPost("Search")]
    public async Task<IActionResult> Search(string procNum, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(procNum))
        {
            TempData["Error"] = "Nomor SPMP atau WO tidak boleh kosong.";
            return RedirectToAction(nameof(Index));
        }

        var tracking = await _trackingService.GetTrackingByProcNumAsync(procNum.Trim(), ct);
        if (tracking == null)
        {
            TempData["Error"] =
                $"Pekerjaan dengan nomor '{procNum}' tidak ditemukan. Coba cari dengan nomor SPMP atau nomor WO.";
            return RedirectToAction(nameof(Index));
        }

        return View("TrackingResult", tracking);
    }

    [HttpGet("Details/{procurementId}")]
    public async Task<IActionResult> Details(string procurementId, CancellationToken ct)
    {
        var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
        if (tracking == null)
        {
            TempData["Error"] = "Procurement tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        return View("TrackingResult", tracking);
    }

    [HttpGet("api/{procNum}")]
    public async Task<IActionResult> GetTrackingApi(string procNum, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(procNum))
            return BadRequest(new { success = false, message = "Nomor Procurement tidak boleh kosong." });

        var tracking = await _trackingService.GetTrackingByProcNumAsync(procNum.Trim(), ct);
        if (tracking == null)
        {
            return NotFound(
                new
                {
                    success = false,
                    message = $"Procurement dengan nomor '{procNum}' tidak ditemukan.",
                }
            );
        }

        return Ok(new { success = true, data = tracking });
    }
}
