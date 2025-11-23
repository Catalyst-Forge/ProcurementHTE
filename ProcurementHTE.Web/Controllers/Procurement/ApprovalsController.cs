using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.ProcurementModule {
    [Authorize]
    public class ApprovalsController : Controller {
        private readonly IApprovalService _approvalService;
        private readonly UserManager<User> _userMgr;
        private readonly ILogger<ApprovalsController> _logger;

        public ApprovalsController(IApprovalService approvalService, UserManager<User> userMgr, ILogger<ApprovalsController> logger) {
            _approvalService = approvalService;
            _userMgr = userMgr;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index() {
            var user = await _userMgr.GetUserAsync(User);
            if (user == null)
                return Challenge();
            var approvals = await _approvalService.GetPendingApprovalsForUserAsync(user);
            return View(approvals);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id) {
            var user = await _userMgr.GetUserAsync(User);
            if (user == null)
                return Challenge();

            try {
                await _approvalService.ApproveAsync(id, user.Id);
                TempData["success"] = "Dokumen disetujui.";
            } catch (Exception ex) {
                _logger.LogError(ex, "Error saat approve {ApprovalId}", id);
                TempData["error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id, string? note) {
            var user = await _userMgr.GetUserAsync(User);
            if (user == null)
                return Challenge();

            try {
                await _approvalService.RejectAsync(id, user.Id, note);
                TempData["error"] = "Dokumen ditolak.";
            } catch (Exception ex) {
                _logger.LogError(ex, "Error saat reject {ApprovalId}", id);
                TempData["error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
