using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.ApiController
{
    [ApiController]
    [Route("api/v1/documents")]
    [Authorize]
    [Produces("application/json")]
    public class DocumentsApiController : ControllerBase
    {
        // NOTE: sementara tetap pakai service query lama (IApprovalServiceApi) untuk paging by QR.
        private readonly IApprovalServiceApi _approvalQuery;
        private readonly IProcDocumentService _docSvc;

        public DocumentsApiController(
            IApprovalServiceApi approvalQuery,
            IProcDocumentService docSvc
        )
        {
            _approvalQuery = approvalQuery;
            _docSvc = docSvc;
        }

        [HttpPost("by-qr")]
        [Consumes("application/json")]
        public async Task<IActionResult> GetByQr(
            [FromBody] ByQrRequest body,
            CancellationToken ct = default
        )
        {
            ArgumentNullException.ThrowIfNull(body);
            if (body is null || string.IsNullOrWhiteSpace(body.QrText))
                return BadRequest(
                    new ProblemDetails { Title = "QrText is required", Status = 400 }
                );

            var qrText = body.QrText.Trim();
            if (qrText.Length > 1024)
                return BadRequest(
                    new ProblemDetails { Title = "qrText too long (max 1024)", Status = 400 }
                );

            var page = Math.Max(1, body.Page ?? 1);
            var pageSize = Math.Clamp(body.PageSize ?? 20, 1, 100);

            var res = await _approvalQuery.GetProcDocumentsByQrTextAsync(
                qrText,
                page,
                pageSize,
                ct
            );
            if (res.TotalItems == 0 || res.Items.Count == 0)
                return NotFound(
                    new ProblemDetails
                    {
                        Title = "Document reference not found or not pending",
                        Detail = "No related reference/procurement document is available.",
                        Status = 404,
                        Instance = HttpContext.Request.Path,
                    }
                );

            // presign untuk setiap item
            var ttl = TimeSpan.FromMinutes(15);
            var list = new List<ProcDocumentLiteWithUrlDto>(res.Items.Count);
            var warnings = new List<string>();

            foreach (var d in res.Items)
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
                    warnings.Add(
                        $"Failed to create view link for document {d.ProcDocumentId}: {ex.Message}"
                    );
                }

                list.Add(
                    new ProcDocumentLiteWithUrlDto(
                        d.ProcDocumentId,
                        d.ProcurementId,
                        d.FileName,
                        d.Status,
                        d.QrText,
                        d.ObjectKey,
                        d.Description,
                        d.CreatedByUserId,
                        d.CreatedByUserName,
                        d.CreatedAt,
                        viewUrl
                    )
                );
            }

            var totalPages = (int)Math.Ceiling((double)res.TotalItems / pageSize);
            var meta = new PagedMeta(page, pageSize, res.TotalItems, totalPages);
            var responseMessage =
                warnings.Count > 0 ? $"OK with warnings: {string.Join(" | ", warnings)}" : "OK";

            return Ok(
                ApiResponse<IReadOnlyList<ProcDocumentLiteWithUrlDto>>.Ok(
                    list,
                    responseMessage,
                    meta
                )
            );
        }

        [HttpPost("resolve-qr")]
        [Consumes("application/json")]
        public async Task<IActionResult> ResolveQr(
            [FromBody] ApiResolveQrRequest body,
            CancellationToken ct = default
        )
        {
            if (body is null || string.IsNullOrWhiteSpace(body.QrText))
                return BadRequest(
                    new ProblemDetails { Title = "QrText is required", Status = 400 }
                );

            var doc = await _approvalQuery.GetProcDocumentByQrCode(body.QrText.Trim(), ct);
            if (doc is null)
                return NotFound(
                    new ProblemDetails { Title = "ProcDocument not found", Status = 404 }
                );

            var viewUrl = await _docSvc.GetPresignedUrlAsync(
                doc.ProcDocumentId,
                TimeSpan.FromMinutes(15)
            );
            var dto = new ProcDocumentLiteWithUrlDto(
                doc.ProcDocumentId,
                doc.ProcurementId,
                doc.FileName,
                doc.Status,
                doc.QrText,
                doc.ObjectKey,
                doc.Description,
                doc.CreatedByUserId,
                doc.CreatedByUserName,
                doc.CreatedAt,
                viewUrl
            );

            return Ok(ApiResponse<ProcDocumentLiteWithUrlDto>.Ok(dto, "OK"));
        }
    }
}
