using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.ApiController
{
    [ApiController]
    [Route("api/v1/approval")]
    [Authorize]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class ApprovalApiController : ControllerBase
    {
        private readonly IApprovalService _svc;
        private readonly UserManager<User> _userMgr;

        public ApprovalApiController(IApprovalService svc, UserManager<User> userMgr)
        {
            _svc = svc;
            _userMgr = userMgr;
        }

        [HttpPost("update-status-by-qrcode")]
        public async Task<IActionResult> UpdateStatusByQrCode(
            [FromBody] UpdateByQrRequest req,
            CancellationToken ct
        )
        {
            if (req is null)
                return BadRequest(new { ok = false, message = "Body tidak boleh kosong." });

            var user = await _userMgr.GetUserAsync(User);
            if (user is null)
                return Unauthorized(new { ok = false, message = "Unauthenticated" });

            ApprovalUpdateResult result = await _svc.UpdateStatusByQrAsync(
                req.QrText ?? "",
                req.Action ?? "",
                req.Note,
                user,
                ct
            );

            return Ok(result);
        }

        [HttpPost("update-status-by-approval-id")]
        public async Task<IActionResult> UpdateStatusByApprovalId(
            [FromBody] UpdateByApprovalIdRequest req,
            CancellationToken ct
        )
        {
            if (req is null)
                return BadRequest(new { ok = false, message = "Body tidak boleh kosong." });

            var user = await _userMgr.GetUserAsync(User);
            if (user is null)
                return Unauthorized(new { ok = false, message = "Unauthenticated" });

            ApprovalUpdateResult result = await _svc.UpdateStatusByApprovalIdAsync(
                req.ProcDocumentApprovalId ?? "",
                req.Action ?? "",
                req.Note,
                user,
                ct
            );

            return Ok(result);
        }

        [HttpPost("update-status-by-document-id")]
        public async Task<IActionResult> UpdateStatusByDocumentId(
            [FromBody] UpdateByProcDocumentIdRequest req,
            CancellationToken ct
        )
        {
            if (req is null)
                return BadRequest(new { ok = false, message = "Body tidak boleh kosong." });

            var user = await _userMgr.GetUserAsync(User);
            if (user is null)
                return Unauthorized(new { ok = false, message = "Unauthenticated" });

            var result = await _svc.UpdateStatusByDocumentIdAsync(
                req.ProcDocumentId ?? "",
                req.Action ?? "",
                req.Note,
                user,
                ct
            );
            return Ok(result);
        }
    }
}
