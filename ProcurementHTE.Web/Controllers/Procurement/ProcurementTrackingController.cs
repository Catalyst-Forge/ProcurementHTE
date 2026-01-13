using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using System.Security.Claims;

namespace ProcurementHTE.Web.Controllers.ProcurementModule
{
    [Authorize]
    [Route("ProcurementTracking")]
    public class ProcurementTrackingController : Controller
    {
        private readonly IProcurementTrackingService _trackingService;
        private readonly ILogger<ProcurementTrackingController> _logger;
        private readonly IQrCodeGenerator _qrCodeGenerator;

        public ProcurementTrackingController(
            IProcurementTrackingService trackingService,
            ILogger<ProcurementTrackingController> logger,
            IQrCodeGenerator qrCodeGenerator
        )
        {
            _trackingService = trackingService;
            _logger = logger;
            _qrCodeGenerator = qrCodeGenerator;
        }

        /// <summary>
        /// Check if current user can modify procurement progress.
        /// Only Admin or APPO user who picked up the procurement can update progress.
        /// </summary>
        private bool CanUserModifyProcurement(ProcurementTrackingDto tracking)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            // Admin can modify any procurement
            if (isAdmin)
                return true;

            // APPO user who picked up can modify their assigned procurement
            if (!string.IsNullOrEmpty(tracking.AppoUserId) && tracking.AppoUserId == userId)
                return true;

            return false;
        }

        /// <summary>
        /// Check if current user can approve the procurement based on current status and role.
        /// Analyst HTE & LTS can approve WaitingApprovalAnalyst, Assistant Manager HTE can approve WaitingApprovalAsstManager, etc.
        /// Admin can approve any waiting approval status.
        /// </summary>
        private bool CanUserApprove(ProcurementTrackingDto tracking)
        {
            var isAdmin = User.IsInRole("Admin");
            var isAnalyst = User.IsInRole("Analyst HTE & LTS");
            var isAsstManager = User.IsInRole("Assistant Manager HTE");
            var isManager = User.IsInRole("Manager Transport & Logistic");

            // Admin can approve any waiting approval status
            if (isAdmin && tracking.IsWaitingApproval)
                return true;

            // Role-based approval matching current status
            return tracking.CurrentStatus switch
            {
                ProcurementHTE.Core.Enums.ProcurementStatus.WaitingApprovalAnalyst => isAnalyst,
                ProcurementHTE.Core.Enums.ProcurementStatus.WaitingApprovalAsstManager => isAsstManager,
                ProcurementHTE.Core.Enums.ProcurementStatus.WaitingApprovalManager => isManager,
                _ => false
            };
        }

        // GET: /ProcurementTracking
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        // POST: /ProcurementTracking/Search
        [HttpPost("Search")]
        public async Task<IActionResult> Search(string procNum, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(procNum))
            {
                TempData["Error"] = "Nomor Procurement tidak boleh kosong.";
                return RedirectToAction(nameof(Index));
            }

            var tracking = await _trackingService.GetTrackingByProcNumAsync(procNum.Trim(), ct);

            if (tracking == null)
            {
                TempData["Error"] = $"Procurement dengan nomor '{procNum}' tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            return View("TrackingResult", tracking);
        }

        // GET: /ProcurementTracking/Details/{procurementId}
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

        // API endpoint for getting tracking data (untuk AJAX/fetch dari frontend)
        [HttpGet("api/{procNum}")]
        public async Task<IActionResult> GetTrackingApi(string procNum, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(procNum))
            {
                return BadRequest(
                    new { success = false, message = "Nomor Procurement tidak boleh kosong." }
                );
            }

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

        // POST: /ProcurementTracking/SendApproval
        [HttpPost("SendApproval")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendApproval(string procurementId, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User tidak teridentifikasi.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Authorization check
            var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
            if (tracking == null)
            {
                TempData["Error"] = "Procurement tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            if (!CanUserModifyProcurement(tracking))
            {
                TempData["Error"] = "Anda tidak memiliki akses untuk mengupdate procurement ini.";
                return RedirectToAction("Details", new { procurementId });
            }

            var result = await _trackingService.SendForApprovalAsync(procurementId, userId, ct);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Details", new { procurementId });
        }

        // POST: /ProcurementTracking/SubmitIspa
        [HttpPost("SubmitIspa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitIspa(
            string procurementId,
            string ispaNumber,
            CancellationToken ct
        )
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User tidak teridentifikasi.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Authorization check
            var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
            if (tracking == null)
            {
                TempData["Error"] = "Procurement tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            if (!CanUserModifyProcurement(tracking))
            {
                TempData["Error"] = "Anda tidak memiliki akses untuk mengupdate procurement ini.";
                return RedirectToAction("Details", new { procurementId });
            }

            var result = await _trackingService.SubmitIspaAsync(
                procurementId,
                ispaNumber,
                userId,
                ct
            );

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Details", new { procurementId });
        }

        // POST: /ProcurementTracking/SubmitJustification
        [HttpPost("SubmitJustification")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitJustification(
            string procurementId,
            IFormFile justificationFile,
            CancellationToken ct
        )
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User tidak teridentifikasi.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Authorization check
            var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
            if (tracking == null)
            {
                TempData["Error"] = "Procurement tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            if (!CanUserModifyProcurement(tracking))
            {
                TempData["Error"] = "Anda tidak memiliki akses untuk mengupdate procurement ini.";
                return RedirectToAction("Details", new { procurementId });
            }

            if (justificationFile == null || justificationFile.Length == 0)
            {
                TempData["Error"] = "File bukti hardcopy tidak boleh kosong.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Validate file size (max 10MB)
            if (justificationFile.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "Ukuran file maksimal 10MB.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Validate file type (images only)
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(justificationFile.ContentType.ToLower()))
            {
                TempData["Error"] = "Format file harus berupa gambar (JPEG, PNG, GIF).";
                return RedirectToAction("Details", new { procurementId });
            }

            var result = await _trackingService.SubmitJustificationAsync(
                procurementId,
                userId,
                justificationFile.FileName,
                justificationFile.ContentType,
                justificationFile.Length,
                justificationFile.OpenReadStream(),
                ct
            );

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Details", new { procurementId });
        }

        // POST: /ProcurementTracking/SubmitPo
        [HttpPost("SubmitPo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPo(
            string procurementId,
            string poNumber,
            CancellationToken ct
        )
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User tidak teridentifikasi.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Authorization check
            var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
            if (tracking == null)
            {
                TempData["Error"] = "Procurement tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            if (!CanUserModifyProcurement(tracking))
            {
                TempData["Error"] = "Anda tidak memiliki akses untuk mengupdate procurement ini.";
                return RedirectToAction("Details", new { procurementId });
            }

            var result = await _trackingService.SubmitPoAsync(procurementId, poNumber, userId, ct);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Details", new { procurementId });
        }

        // POST: /ProcurementTracking/Approve
        [HttpPost("Approve")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Analyst HTE & LTS, Assistant Manager HTE, Manager Transport & Logistic")]
        public async Task<IActionResult> Approve(
            string procurementId,
            string? approvalNote,
            CancellationToken ct
        )
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User tidak teridentifikasi.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Authorization check
            var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
            if (tracking == null)
            {
                TempData["Error"] = "Procurement tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user can approve based on current status
            var canApprove = CanUserApprove(tracking);
            if (!canApprove)
            {
                TempData["Error"] = "Anda tidak memiliki akses untuk approve procurement ini.";
                return RedirectToAction("Details", new { procurementId });
            }

            var result = await _trackingService.HandleApprovalStatusChangeAsync(
                procurementId,
                "approve",
                userId,
                approvalNote ?? "Approved via web",
                ct
            );

            if (result)
            {
                TempData["Success"] = "Procurement berhasil di-approve.";
            }
            else
            {
                TempData["Error"] = "Gagal melakukan approval. Silakan coba lagi.";
            }

            return RedirectToAction("Details", new { procurementId });
        }

        // POST: /ProcurementTracking/Reject
        [HttpPost("Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            string procurementId,
            string rejectionNote,
            CancellationToken ct
        )
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User tidak teridentifikasi.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Authorization check
            var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
            if (tracking == null)
            {
                TempData["Error"] = "Procurement tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            if (!CanUserModifyProcurement(tracking))
            {
                TempData["Error"] = "Anda tidak memiliki akses untuk mengupdate procurement ini.";
                return RedirectToAction("Details", new { procurementId });
            }

            var result = await _trackingService.RejectProcurementAsync(
                procurementId,
                rejectionNote,
                userId,
                ct
            );

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Details", new { procurementId });
        }

        // GET: /ProcurementTracking/GenerateQrCode/{procurementId}
        [HttpGet("GenerateQrCode/{procurementId}")]
        public async Task<IActionResult> GenerateQrCode(string procurementId, CancellationToken ct)
        {
            var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);

            if (tracking == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(tracking.ApprovalToken))
            {
                return BadRequest("Approval token tidak tersedia.");
            }

            // Generate QR code using local library (no external API)
            var deepLink = $"procurehte://approve/{tracking.ApprovalToken}";
            var pngBytes = _qrCodeGenerator.GenerateAsPng(deepLink, 10);

            return File(pngBytes, "image/png", "approval-qr.png");
        }

        /// <summary>
        /// Get presigned URL for hardcopy evidence image from MinIO
        /// </summary>
        [HttpGet("HardcopyEvidence/{procurementId}")]
        public async Task<IActionResult> GetHardcopyEvidence(string procurementId, CancellationToken ct)
        {
            var url = await _trackingService.GetHardcopyEvidenceUrlAsync(procurementId, ct);
            if (string.IsNullOrEmpty(url))
            {
                return NotFound("Hardcopy evidence tidak ditemukan.");
            }

            return Json(new { url });
        }
    }
}
