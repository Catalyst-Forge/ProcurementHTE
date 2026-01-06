using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.ApiController
{
    [ApiController]
    [Route("api/v1/approval")]
    [Authorize]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class ApprovalApiController : ControllerBase
    {
        private readonly UserManager<User> _userMgr;
        private readonly IPurchaseRequisitionTrackingService _trackingSvc;
        private readonly IPurchaseRequisitionRepository _prRepo;

        public ApprovalApiController(
            UserManager<User> userMgr,
            IPurchaseRequisitionTrackingService trackingSvc,
            IPurchaseRequisitionRepository prRepo)
        {
            _userMgr = userMgr;
            _trackingSvc = trackingSvc;
            _prRepo = prRepo;
        }

        // ============================================================
        // PR Approval Endpoints (approval per PR, bukan per dokumen)
        // ============================================================

        /// <summary>
        /// Get PR tracking info (for Flutter after QR scan)
        /// QR format: procurehte://approve/{prId}
        /// </summary>
        [HttpGet("pr/{prId}")]
        public async Task<IActionResult> GetPrApprovalInfo(string prId, CancellationToken ct)
        {
            var user = await _userMgr.GetUserAsync(User);
            if (user is null)
                return Unauthorized(new { ok = false, message = "Unauthenticated" });

            var tracking = await _trackingSvc.GetTrackingByPrIdAsync(prId, ct);
            if (tracking == null)
                return NotFound(new { ok = false, message = "PR tidak ditemukan" });

            // Get user's roles
            var userRoles = await _userMgr.GetRolesAsync(user);
            var canApprove = CanUserApproveStatus(tracking.CurrentStatus, userRoles);

            return Ok(new
            {
                ok = true,
                data = new
                {
                    prId = tracking.PrId,
                    prNumber = tracking.PrNumber,
                    description = tracking.Description,
                    requestDate = tracking.RequestDate,
                    createdBy = tracking.CreatedByFullName ?? tracking.CreatedByUserName,
                    currentStatus = tracking.CurrentStatus.ToString(),
                    currentStatusDisplay = tracking.CurrentStatusDescription,
                    isWaitingApproval = tracking.IsWaitingApproval,
                    nextApproverRole = tracking.NextApproverRole,
                    linkedProcurementsCount = tracking.LinkedProcurementsCount,
                    totalMandatoryDocuments = tracking.TotalMandatoryDocs,
                    uploadedMandatoryDocuments = tracking.UploadedMandatoryDocs,
                    progressPercentage = tracking.ProgressPercentage,
                    canApprove,
                    currentUserRoles = userRoles.ToList()
                }
            });
        }

        /// <summary>
        /// Get list of documents in a PR (for Flutter to view documents - read only)
        /// </summary>
        [HttpGet("pr/{prId}/documents")]
        public async Task<IActionResult> GetPrDocuments(string prId, CancellationToken ct)
        {
            var user = await _userMgr.GetUserAsync(User);
            if (user is null)
                return Unauthorized(new { ok = false, message = "Unauthenticated" });

            // Get PR with procurements and documents
            var pr = await _prRepo.GetByIdWithProcurementsAsync(prId, ct);
            if (pr == null)
                return NotFound(new { ok = false, message = "PR tidak ditemukan" });

            // Flatten all documents from all procurements
            var documents = pr.Procurements
                .SelectMany(p => p.ProcDocuments ?? Enumerable.Empty<ProcDocuments>(), (proc, doc) => new
                {
                    procDocumentId = doc.ProcDocumentId,
                    procurementId = proc.ProcurementId,
                    procurementName = proc.JobName,
                    procurementNum = proc.ProcNum,
                    documentTypeName = doc.DocumentType?.Name ?? "Unknown",
                    fileName = doc.FileName,
                    status = doc.Status,
                    createdAt = doc.CreatedAt,
                    qrText = doc.QrText
                })
                .OrderBy(d => d.procurementName)
                .ThenBy(d => d.documentTypeName)
                .ToList();

            return Ok(new 
            { 
                ok = true, 
                prId,
                prNumber = pr.PrNumber,
                data = documents, 
                count = documents.Count 
            });
        }

        /// <summary>
        /// Approve PR - maju ke step approval berikutnya sesuai role
        /// Flow: WaitingApprovalAnalyst → WaitingApprovalAsstManager → WaitingApprovalManager → OnSubmitISPA
        /// </summary>
        [HttpPost("pr/{prId}/approve")]
        public async Task<IActionResult> ApprovePr(string prId, [FromBody] PrApprovalRequest? req, CancellationToken ct)
        {
            var user = await _userMgr.GetUserAsync(User);
            if (user is null)
                return Unauthorized(new { ok = false, message = "Unauthenticated" });

            // Get PR tracking info
            var tracking = await _trackingSvc.GetTrackingByPrIdAsync(prId, ct);
            if (tracking == null)
                return NotFound(new { ok = false, message = "PR tidak ditemukan" });

            // Check if PR is waiting approval
            if (!tracking.IsWaitingApproval)
                return BadRequest(new { ok = false, message = "PR tidak dalam status menunggu approval", currentStatus = tracking.CurrentStatus.ToString() });

            // Check user role matches required approver
            var userRoles = await _userMgr.GetRolesAsync(user);
            var canApprove = CanUserApproveStatus(tracking.CurrentStatus, userRoles);
            
            if (!canApprove)
                return BadRequest(new { 
                    ok = false, 
                    message = "Anda tidak memiliki role untuk approve status ini", 
                    requiredRole = tracking.NextApproverRole,
                    yourRoles = userRoles.ToList()
                });

            // Perform approval - move to next status
            var success = await _trackingSvc.HandleApprovalStatusChangeAsync(
                prId, 
                "approve", 
                user.Id, 
                req?.Note, 
                ct
            );

            if (!success)
                return BadRequest(new { ok = false, message = "Gagal melakukan approval" });

            // Get updated tracking
            var updatedTracking = await _trackingSvc.GetTrackingByPrIdAsync(prId, ct);

            return Ok(new
            {
                ok = true,
                message = "PR berhasil di-approve",
                data = new
                {
                    prId,
                    prNumber = updatedTracking?.PrNumber,
                    previousStatus = tracking.CurrentStatus.ToString(),
                    newStatus = updatedTracking?.CurrentStatus.ToString(),
                    newStatusDisplay = updatedTracking?.CurrentStatusDescription,
                    approvedBy = user.FullName ?? user.UserName,
                    approvedAt = DateTime.UtcNow
                }
            });
        }

        /// <summary>
        /// Reject PR - status menjadi Rejected
        /// </summary>
        [HttpPost("pr/{prId}/reject")]
        public async Task<IActionResult> RejectPr(string prId, [FromBody] PrApprovalRequest? req, CancellationToken ct)
        {
            var user = await _userMgr.GetUserAsync(User);
            if (user is null)
                return Unauthorized(new { ok = false, message = "Unauthenticated" });

            if (string.IsNullOrWhiteSpace(req?.Note))
                return BadRequest(new { ok = false, message = "Alasan penolakan (note) wajib diisi" });

            // Get PR tracking info
            var tracking = await _trackingSvc.GetTrackingByPrIdAsync(prId, ct);
            if (tracking == null)
                return NotFound(new { ok = false, message = "PR tidak ditemukan" });

            // Check if PR is waiting approval
            if (!tracking.IsWaitingApproval)
                return BadRequest(new { ok = false, message = "PR tidak dalam status menunggu approval", currentStatus = tracking.CurrentStatus.ToString() });

            // Check user role matches required approver
            var userRoles = await _userMgr.GetRolesAsync(user);
            var canApprove = CanUserApproveStatus(tracking.CurrentStatus, userRoles);
            
            if (!canApprove)
                return BadRequest(new { 
                    ok = false, 
                    message = "Anda tidak memiliki role untuk reject status ini", 
                    requiredRole = tracking.NextApproverRole,
                    yourRoles = userRoles.ToList()
                });

            // Perform rejection
            var result = await _trackingSvc.RejectPrAsync(prId, req.Note, user.Id, ct);

            if (!result.Success)
                return BadRequest(new { ok = false, message = result.Message ?? "Gagal melakukan rejection" });

            return Ok(new
            {
                ok = true,
                message = "PR ditolak",
                data = new
                {
                    prId,
                    prNumber = tracking.PrNumber,
                    previousStatus = tracking.CurrentStatus.ToString(),
                    newStatus = "Rejected",
                    rejectedBy = user.FullName ?? user.UserName,
                    rejectedAt = DateTime.UtcNow,
                    rejectionNote = req.Note
                }
            });
        }

        /// <summary>
        /// Check if user role can approve current PR status
        /// </summary>
        private static bool CanUserApproveStatus(ProcurementHTE.Core.Enums.PurchaseRequisitionStatus status, IList<string> userRoles)
        {
            return status switch
            {
                ProcurementHTE.Core.Enums.PurchaseRequisitionStatus.WaitingApprovalAnalyst => 
                    userRoles.Contains("Analyst HTE") || userRoles.Contains("Admin"),
                ProcurementHTE.Core.Enums.PurchaseRequisitionStatus.WaitingApprovalAsstManager => 
                    userRoles.Contains("Asst Manager") || userRoles.Contains("Admin"),
                ProcurementHTE.Core.Enums.PurchaseRequisitionStatus.WaitingApprovalManager => 
                    userRoles.Contains("Manager") || userRoles.Contains("Admin"),
                _ => false
            };
        }
    }

    /// <summary>
    /// Request body untuk approve/reject PR
    /// </summary>
    public class PrApprovalRequest
    {
        public string? Note { get; set; }
    }
}
