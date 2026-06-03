using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using System.Security.Claims;

namespace ProcurementHTE.Web.Controllers.ProcurementModule
{
    [Authorize]
    [Route("ProcurementTracking")]
    public class ProcurementTrackingController : Controller
    {
        private readonly IProcurementTrackingService _trackingService;
        private readonly IDashboardService _dashboardService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ProcurementTrackingController> _logger;
        private readonly IQrCodeGenerator _qrCodeGenerator;

        public ProcurementTrackingController(
            IProcurementTrackingService trackingService,
            IDashboardService dashboardService,
            UserManager<User> userManager,
            ILogger<ProcurementTrackingController> logger,
            IQrCodeGenerator qrCodeGenerator
        )
        {
            _trackingService = trackingService;
            _dashboardService = dashboardService;
            _userManager = userManager;
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

        /// <summary>
        /// Check if current user has any approver role
        /// </summary>
        private bool HasApproverRole()
        {
            return User.IsInRole("Admin") ||
                   User.IsInRole("Analyst HTE & LTS") ||
                   User.IsInRole("Assistant Manager HTE") ||
                   User.IsInRole("Manager Transport & Logistic");
        }

        // GET: /ProcurementTracking
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /ProcurementTracking/PendingApprovals
        [HttpGet("PendingApprovals")]
        public async Task<IActionResult> PendingApprovals(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15,
            [FromQuery] bool partial = false,
            CancellationToken ct = default
        )
        {
            if (!HasApproverRole())
            {
                TempData["Error"] = "Anda tidak memiliki akses untuk melihat pending approvals.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var rolesArray = roles.ToArray();

            var skip = (page - 1) * pageSize;
            var (items, totalCount) = await _dashboardService.GetPendingApprovalsByUserAsync(
                userId,
                rolesArray,
                skip,
                pageSize,
                ct
            );

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Return partial view for explicit partial requests (pagination, refresh button)
            // or HTMX requests that are NOT boosted navigation
            var isHtmxRequest = Request.Headers["HX-Request"].Count > 0;
            var isBoosted = Request.Headers["HX-Boosted"].Count > 0;
            
            if (partial || (isHtmxRequest && !isBoosted))
            {
                return PartialView("_PendingApprovalsTable", items);
            }

            return View(items);
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
                TempData["Error"] = $"Procurement dengan nomor '{procNum}' tidak ditemukan. Coba cari dengan Proc Number, WO Number, atau SPMP Number.";
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
            DateTime ispaDate,
            DateTime ispaSubmitDate,
            IFormFile ispaFile,
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

            // Validate file is provided
            if (ispaFile == null || ispaFile.Length == 0)
            {
                TempData["Error"] = "File dokumen ISPA wajib diupload.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Validate file type (PDF only)
            var ext = Path.GetExtension(ispaFile.FileName).ToLowerInvariant();
            var isPdf = ext == ".pdf" || ispaFile.ContentType.ToLower() == "application/pdf";
            if (!isPdf)
            {
                TempData["Error"] = "Format file ISPA harus PDF.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Validate file size (max 50MB)
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            if (ispaFile.Length > maxFileSize)
            {
                TempData["Error"] = "Ukuran file ISPA maksimal 50MB.";
                return RedirectToAction("Details", new { procurementId });
            }

            var result = await _trackingService.SubmitIspaAsync(
                procurementId,
                ispaNumber,
                userId,
                ispaDate,
                ispaSubmitDate,
                ispaFile.FileName,
                ispaFile.ContentType,
                ispaFile.Length,
                ispaFile.OpenReadStream(),
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

        // POST: /ProcurementTracking/Reject (LEGACY - final rejection)
        [HttpPost("Reject")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Analyst HTE & LTS, Assistant Manager HTE, Manager Transport & Logistic")]
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

            // Check if user can reject based on current status (same as approve permission)
            var canReject = CanUserApprove(tracking);
            if (!canReject)
            {
                TempData["Error"] = "Anda tidak memiliki akses untuk reject procurement ini.";
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

        // POST: /ProcurementTracking/ReturnForRevision
        [HttpPost("ReturnForRevision")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Analyst HTE & LTS, Assistant Manager HTE, Manager Transport & Logistic")]
        public async Task<IActionResult> ReturnForRevision(
            string procurementId,
            int[] symptoms,
            string rejectionNote,
            CancellationToken ct
        )
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    return Json(new { success = false, message = "User tidak teridentifikasi." });
                TempData["Error"] = "User tidak teridentifikasi.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Authorization check
            var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
            if (tracking == null)
            {
                if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    return Json(new { success = false, message = "Procurement tidak ditemukan." });
                TempData["Error"] = "Procurement tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            var canReject = CanUserApprove(tracking);
            if (!canReject)
            {
                if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    return Json(new { success = false, message = "Anda tidak memiliki akses untuk reject procurement ini." });
                TempData["Error"] = "Anda tidak memiliki akses untuk reject procurement ini.";
                return RedirectToAction("Details", new { procurementId });
            }

            // Combine symptoms array into flags enum
            var symptomFlags = RejectionSymptom.None;
            foreach (var s in symptoms)
            {
                symptomFlags |= (RejectionSymptom)s;
            }

            if (symptomFlags == RejectionSymptom.None)
            {
                if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    return Json(new { success = false, message = "Minimal satu symptom harus dipilih." });
                TempData["Error"] = "Minimal satu symptom harus dipilih.";
                return RedirectToAction("Details", new { procurementId });
            }

            var result = await _trackingService.ReturnForRevisionAsync(
                procurementId,
                symptomFlags,
                rejectionNote,
                userId,
                ct
            );

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return Json(new { success = result.Success, message = result.Message });
            }

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

        // POST: /ProcurementTracking/ResubmitRevision
        [HttpPost("ResubmitRevision")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Operation, Analyst HTE & LTS, AP-PO")]
        public async Task<IActionResult> ResubmitRevision(
            string procurementId,
            CancellationToken ct
        )
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    return Json(new { success = false, message = "User tidak teridentifikasi." });
                TempData["Error"] = "User tidak teridentifikasi.";
                return RedirectToAction("Details", new { procurementId });
            }

            var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
            if (tracking == null)
            {
                if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    return Json(new { success = false, message = "Procurement tidak ditemukan." });
                TempData["Error"] = "Procurement tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user can resubmit based on current status
            var canResubmit = CanUserResubmitRevision(tracking);
            if (!canResubmit)
            {
                if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    return Json(new { success = false, message = "Anda tidak memiliki akses untuk resubmit revision ini." });
                TempData["Error"] = "Anda tidak memiliki akses untuk resubmit revision ini.";
                return RedirectToAction("Details", new { procurementId });
            }

            var result = await _trackingService.ResubmitRevisionAsync(
                procurementId,
                userId,
                ct
            );

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return Json(new { success = result.Success, message = result.Message });
            }

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

        /// <summary>
        /// Check if current user can resubmit revision based on current status.
        /// NeedsRevisionData: PIC Ops (Operation) or Admin
        /// NeedsRevisionPR: APPO or Admin
        /// </summary>
        private bool CanUserResubmitRevision(ProcurementTrackingDto tracking)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (isAdmin)
                return true;

            return tracking.CurrentStatus switch
            {
                ProcurementStatus.NeedsRevisionData => 
                    User.IsInRole("Operation") || User.IsInRole("Analyst HTE & LTS") ||
                    (!string.IsNullOrEmpty(tracking.PicOpsUserId) && tracking.PicOpsUserId == userId),
                ProcurementStatus.NeedsRevisionPR => 
                    User.IsInRole("AP-PO") ||
                    (!string.IsNullOrEmpty(tracking.AppoUserId) && tracking.AppoUserId == userId),
                _ => false
            };
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

        /// <summary>
        /// Download ISPA file from MinIO
        /// </summary>
        [HttpGet("DownloadIspaFile/{procurementId}")]
        public async Task<IActionResult> DownloadIspaFile(string procurementId, CancellationToken ct)
        {
            var url = await _trackingService.GetIspaFileUrlAsync(procurementId, ct);
            if (string.IsNullOrEmpty(url))
            {
                TempData["Error"] = "File ISPA tidak ditemukan.";
                return RedirectToAction("Details", new { procurementId });
            }

            return Redirect(url);
        }
    }
}
