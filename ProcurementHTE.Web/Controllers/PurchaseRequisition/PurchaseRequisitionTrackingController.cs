using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.PR
{
    [Authorize]
    [Route("PRTracking")]
    public class PurchaseRequisitionTrackingController : Controller
    {
        private readonly IPurchaseRequisitionTrackingService _trackingService;
        private readonly ILogger<PurchaseRequisitionTrackingController> _logger;

        public PurchaseRequisitionTrackingController(
            IPurchaseRequisitionTrackingService trackingService,
            ILogger<PurchaseRequisitionTrackingController> logger
        )
        {
            _trackingService = trackingService;
            _logger = logger;
        }

        // GET: /PRTracking
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        // POST: /PRTracking/Search
        [HttpPost("Search")]
        public async Task<IActionResult> Search(string prNumber, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(prNumber))
            {
                TempData["Error"] = "Nomor PR tidak boleh kosong.";
                return RedirectToAction(nameof(Index));
            }

            var tracking = await _trackingService.GetTrackingByPrNumberAsync(prNumber.Trim(), ct);

            if (tracking == null)
            {
                TempData["Error"] = $"PR dengan nomor '{prNumber}' tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            return View("TrackingResult", tracking);
        }

        // GET: /PRTracking/Details/{prId}
        [HttpGet("Details/{prId}")]
        public async Task<IActionResult> Details(string prId, CancellationToken ct)
        {
            var tracking = await _trackingService.GetTrackingByPrIdAsync(prId, ct);

            if (tracking == null)
            {
                TempData["Error"] = "PR tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            return View("TrackingResult", tracking);
        }

        // API endpoint for getting tracking data (untuk AJAX/fetch dari frontend)
        [HttpGet("api/{prNumber}")]
        public async Task<IActionResult> GetTrackingApi(string prNumber, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(prNumber))
            {
                return BadRequest(new { success = false, message = "Nomor PR tidak boleh kosong." });
            }

            var tracking = await _trackingService.GetTrackingByPrNumberAsync(prNumber.Trim(), ct);

            if (tracking == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"PR dengan nomor '{prNumber}' tidak ditemukan.",
                });
            }

            return Ok(new { success = true, data = tracking });
        }
    }
}
