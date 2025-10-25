using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

[ApiController]
[Route("api/[controller]")]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalServiceApi _svc;

    public ApprovalController(IApprovalServiceApi svc) => _svc = svc;

    [HttpGet("WoDocsByQr")]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<WoDocumentLiteDto>>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WoDocsByQr(
        [FromQuery, BindRequired] string qrText,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(qrText))
            return BadRequest(new ProblemDetails { Title = "qrText is required", Status = 400 });

        if (qrText.Length > 1024)
            return BadRequest(
                new ProblemDetails { Title = "qrText too long (max 1024)", Status = 400 }
            );

        // Normalisasi nilai
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _svc.GetWoDocumentsByQrTextAsync(qrText, page, pageSize, ct);

        if (result.TotalItems == 0 || result.Items.Count == 0)
            return NotFound(
                new ProblemDetails
                {
                    Title = "Document reference not found or not pending",
                    Detail = "Tidak ada dokumen referensi/WO terkait.",
                    Status = 404,
                    Instance = HttpContext.Request.Path,
                }
            );

        var totalPages = (int)Math.Ceiling((double)result.TotalItems / pageSize);
        var meta = new PagedMeta(page, pageSize, result.TotalItems, totalPages);

        return Ok(ApiResponse<IReadOnlyList<WoDocumentLiteDto>>.Ok(result.Items, "OK", meta));
    }

    // PUT /api/approval/wo-documents/{woDocumentId}/status
    [HttpPut("wo-documents/{woDocumentId}/status")]
    [ProducesResponseType(typeof(ApiResponse<WoDocumentLiteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute, BindRequired] string woDocumentId,
        [FromBody, BindRequired] ApiUpdateWoDocStatusRequest body,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(body.Status))
            return BadRequest(new ProblemDetails { Title = "Status is required", Status = 400 });

        if (!DocStatuses.All.Contains(body.Status))
            return BadRequest(
                new ProblemDetails { Title = $"Unknown status '{body.Status}'", Status = 400 }
            );

        try
        {
            var updated = await _svc.UpdateWoDocumentStatusAsync(
                woDocumentId,
                body.Status,
                body.Reason,
                body.ApprovedByUserId,
                ct
            );
            if (updated is null)
                return NotFound(
                    new ProblemDetails
                    {
                        Title = "WoDocument not found",
                        Status = 404,
                        Instance = HttpContext.Request.Path,
                    }
                );

            return Ok(ApiResponse<WoDocumentLiteDto>.Ok(updated, "Status updated"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Transition"))
        {
            return Conflict(
                new ProblemDetails
                {
                    Title = "Invalid status transition",
                    Detail = ex.Message,
                    Status = 409,
                    Instance = HttpContext.Request.Path
                }
            );
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("constraint", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(
                new ProblemDetails
                {
                    Title = "Status conflict",
                    Detail =
                        "Constraint unik bertabrakan untuk (WorkOrderId, DocumentTypeId, Status).",
                    Status = 409,
                    Instance = HttpContext.Request.Path,
                }
            );
        }
    }
}
