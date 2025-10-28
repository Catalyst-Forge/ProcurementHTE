using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

[ApiController]
[Route("api/v1/[controller]")]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalServiceApi _svc;
    private readonly IWoDocumentService _docSvc;

    public ApprovalController(
        IApprovalServiceApi svc,
        IWoDocumentService docSvc
    ) // << add param
        => (_svc, _docSvc) = (svc, docSvc);

    // GET - tetap ada (querystring)
    //[Authorize]
    [HttpGet("WoDocsByQr")]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<WoDocumentLiteWithUrlDto>>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> WoDocsByQr(
        [FromQuery, BindRequired] string qrText,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default
    ) => HandleWoDocsByQr(qrText, page, pageSize, ct);

    //[Authorize]
    [HttpPost("WoDocsByQr")]
    [Consumes("application/json", "application/x-www-form-urlencoded", "multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WoDocumentLiteWithUrlDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WoDocsByQrPostUnified(CancellationToken ct = default)
    {
        WoDocsByQrRequest? req = null;

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(ct);
            req = new WoDocsByQrRequest
            {
                QrText = form["QrText"],
                Page = int.TryParse(form["Page"], out var p) ? p : 1,
                PageSize = int.TryParse(form["PageSize"], out var ps) ? ps : 20
            };
        }
        else
        {
            // JSON
            Request.EnableBuffering();
            using var sr = new StreamReader(Request.Body, leaveOpen: true);
            var json = await sr.ReadToEndAsync();
            Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(json))
            {
                req = System.Text.Json.JsonSerializer.Deserialize<WoDocsByQrRequest>(
                    json,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
        }

        if (req is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = new[] { "Body/form payload is required." }
            })
            { Status = StatusCodes.Status400BadRequest });
        }

        return await HandleWoDocsByQr(
            req.QrText ?? string.Empty,
            req.Page <= 0 ? 1 : req.Page,
            req.PageSize <= 0 ? 20 : req.PageSize,
            ct
        );
    }


    // Handler bersama
    private async Task<IActionResult> HandleWoDocsByQr(
        string qrText,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(qrText))
            return BadRequest(new ProblemDetails { Title = "qrText is required", Status = 400 });

        qrText = qrText.Trim();
        if (qrText.Length > 1024)
            return BadRequest(new ProblemDetails { Title = "qrText too long (max 1024)", Status = 400 });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _svc.GetWoDocumentsByQrTextAsync(qrText, page, pageSize, ct);
        if (result.TotalItems == 0 || result.Items.Count == 0)
            return NotFound(new ProblemDetails
            {
                Title = "Document reference not found or not pending",
                Detail = "Tidak ada dokumen referensi/WO terkait.",
                Status = 404,
                Instance = HttpContext.Request.Path,
            });

        var ttl = TimeSpan.FromMinutes(15);
        var list = new List<WoDocumentLiteWithUrlDto>(result.Items.Count);

        // ⬇️ Loop SEKUENSIAL + presign by ObjectKey (tanpa query DB)
        foreach (var d in result.Items)
        {
            string? viewUrl = null;
            try
            {
                viewUrl = await _docSvc.GetPresignedViewUrlByObjectKeyAsync(
                    d.ObjectKey,
                    d.FileName,
                    "application/pdf",
                    ttl,
                    ct
                );
            }
            catch (Exception ex)
            {
                // Pakai _logger jika kamu inject ILogger<ApprovalController> di ctor
                // _logger.LogWarning(ex, "[WoDocsByQr] Presign gagal: {DocId}", d.WoDocumentId);

                // Atau fallback ambil logger dari DI kalau belum ada field _logger:
                HttpContext.RequestServices
                    .GetRequiredService<ILogger<ApprovalController>>()
                    .LogWarning(ex, "[WoDocsByQr] Presign gagal: {DocId}", d.WoDocumentId);
            }

            list.Add(new WoDocumentLiteWithUrlDto(
                d.WoDocumentId,
                d.WorkOrderId,
                d.FileName,
                d.Status,
                d.QrText,
                d.ObjectKey,
                d.Description,
                d.CreatedByUserId,
                d.CreatedAt,
                viewUrl
            ));
        }

        var totalPages = (int)Math.Ceiling((double)result.TotalItems / pageSize);
        var meta = new PagedMeta(page, pageSize, result.TotalItems, totalPages);

        return Ok(ApiResponse<IReadOnlyList<WoDocumentLiteWithUrlDto>>.Ok(list, "OK", meta));
    }


    // POST JSON (disarankan)
    //[Authorize]
    [HttpPost("wo-documents/status")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<WoDocumentLiteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> UpdateStatusJson(
        [FromBody, BindRequired] ApiUpdateWoDocStatusRequest body,
        CancellationToken ct = default
    ) => HandleUpdateStatus(body, ct);

    // POST Form (opsional, supaya 1 URL bisa form juga)
    //[Authorize]
    [HttpPost("wo-documents/status")]
    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<WoDocumentLiteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> UpdateStatusForm(
        [FromForm, BindRequired] ApiUpdateWoDocStatusRequest body,
        CancellationToken ct = default
    ) => HandleUpdateStatus(body, ct);

    private async Task<IActionResult> HandleUpdateStatus(
        ApiUpdateWoDocStatusRequest body,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(body.WoDocumentId))
            return BadRequest(
                new ProblemDetails { Title = "WoDocumentId is required", Status = 400 }
            );

        if (string.IsNullOrWhiteSpace(body.Status))
            return BadRequest(new ProblemDetails { Title = "Status is required", Status = 400 });

        if (!DocStatuses.All.Contains(body.Status))
            return BadRequest(
                new ProblemDetails { Title = $"Unknown status '{body.Status}'", Status = 400 }
            );

        try
        {
            var updated = await _svc.UpdateWoDocumentStatusAsync(
                body.WoDocumentId.Trim(),
                body.Status.Trim(),
                string.IsNullOrWhiteSpace(body.Reason) ? null : body.Reason.Trim(),
                string.IsNullOrWhiteSpace(body.ApprovedByUserId)
                    ? null
                    : body.ApprovedByUserId.Trim(),
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
                    Instance = HttpContext.Request.Path,
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

    //[Authorize]
    [HttpPost("wo-documents/get-by-qrcode")]
    [Consumes("application/json", "application/x-www-form-urlencoded", "multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<WoDocumentLiteWithUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetWoDocumentByQrCode(
        [FromBody, BindRequired] ApiGetWoDocByQrCodeRequest body,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(body.QrText))
            return BadRequest(new ProblemDetails { Title = "QrText is required", Status = 400 });

        var doc = await _svc.GetWoDocumentByQrCode(body.QrText.Trim(), ct);
        if (doc is null)
            return NotFound(
                new ProblemDetails
                {
                    Title = "WoDocument not found",
                    Status = 404,
                    Instance = HttpContext.Request.Path,
                }
            );

        // 🔑 Ambil presigned VIEW URL berbasis WoDocumentId (inline PDF)
        var viewUrl = await _docSvc.GetPresignedUrlAsync(
            doc.WoDocumentId,
            TimeSpan.FromMinutes(15)
        );

        // Map ke DTO turunan (semua field base + ViewUrl)
        var dto = new WoDocumentLiteWithUrlDto(
            doc.WoDocumentId,
            doc.WorkOrderId,
            doc.FileName,
            doc.Status,
            doc.QrText,
            doc.ObjectKey,
            doc.Description,
            doc.CreatedByUserId,
            doc.CreatedAt,
            viewUrl
        );

        return Ok(ApiResponse<WoDocumentLiteWithUrlDto>.Ok(dto, "OK"));
    }

    //[Authorize]
    [HttpPost("wo-documents/update-status-by-qrcode")]
    [Consumes("application/json", "application/x-www-form-urlencoded", "multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<WoDocumentLiteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatusByQrCode(
        [FromBody, BindRequired] ApiUpdateWoDocStatusByQrCode body,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(body.QrText))
            return BadRequest(new ProblemDetails { Title = "QrText is required", Status = 400 });
        if (string.IsNullOrWhiteSpace(body.Status))
            return BadRequest(new ProblemDetails { Title = "Status is required", Status = 400 });
        if (!DocStatuses.All.Contains(body.Status))
            return BadRequest(
                new ProblemDetails { Title = $"Unknown status '{body.Status}'", Status = 400 }
            );
        var doc = await _svc.GetWoDocumentByQrCode(body.QrText.Trim(), ct);
        if (doc is null)
            return NotFound(
                new ProblemDetails
                {
                    Title = "WoDocument not found",
                    Status = 404,
                    Instance = HttpContext.Request.Path,
                }
            );
        try
        {
            var updated = await _svc.UpdateWoDocumentStatusAsync(
                doc.WoDocumentId,
                body.Status.Trim(),
                string.IsNullOrWhiteSpace(body.Reason) ? null : body.Reason.Trim(),
                string.IsNullOrWhiteSpace(body.ApprovedByUserId)
                    ? null
                    : body.ApprovedByUserId.Trim(),
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
                    Instance = HttpContext.Request.Path,
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
