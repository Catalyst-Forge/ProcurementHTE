using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.ApiController
{
    [ApiController]
    [Route("api/v1/documents")]
    [Authorize] // kalau perlu publik, pindah ke endpoint tertentu
    [Produces("application/json")]
    public class DocumentsApiController : ControllerBase
    {
        private readonly ILogger<DocumentsApiController> _logger;
        // NOTE: sementara tetap pakai service query lama (IApprovalServiceApi) untuk paging by QR.
        // Best-practice ke depan: pindahkan ke IWoDocumentService (read/query).
        private readonly IApprovalServiceApi 
        _approvalQuery;
        private readonly IWoDocumentService _docSvc;

        public DocumentsApiController(
            ILogger<DocumentsApiController> logger,
            IApprovalServiceApi approvalQuery,
            IWoDocumentService docSvc)
        {
            _logger = logger;
            _approvalQuery = approvalQuery;
            _docSvc = docSvc;
        }

        // ====== A) POST by-qr (dulunya GET) ======
        // POST /api/v1/documents/by-qr
        // Body:
        // { "QrText": "...", "Page": 1, "PageSize": 20 }

        [HttpPost("by-qr")]
        [Consumes("application/json")]
        public async Task<IActionResult> GetByQr([FromBody] ByQrRequest body, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(body);
            if (body is null || string.IsNullOrWhiteSpace(body.QrText))
                return BadRequest(new ProblemDetails { Title = "QrText is required", Status = 400 });

            var qrText = body.QrText.Trim();
            if (qrText.Length > 1024)
                return BadRequest(new ProblemDetails { Title = "qrText too long (max 1024)", Status = 400 });

            var page = Math.Max(1, body.Page ?? 1);
            var pageSize = Math.Clamp(body.PageSize ?? 20, 1, 100);

            var res = await _approvalQuery.GetWoDocumentsByQrTextAsync(qrText, page, pageSize, ct);
            if (res.TotalItems == 0 || res.Items.Count == 0)
                return NotFound(new ProblemDetails
                {
                    Title = "Document reference not found or not pending",
                    Detail = "Tidak ada dokumen referensi/WO terkait.",
                    Status = 404,
                    Instance = HttpContext.Request.Path
                });

            // presign untuk setiap item
            var ttl = TimeSpan.FromMinutes(15);
            var list = new List<WoDocumentLiteWithUrlDto>(res.Items.Count);

            foreach (var d in res.Items)
            {
                string? viewUrl = null;
                try
                {
                    viewUrl = await _docSvc.GetPresignedViewUrlByObjectKeyAsync(
                        d.ObjectKey, d.FileName, "application/pdf", ttl, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[documents/by-qr] presign gagal: {DocId}", d.WoDocumentId);
                }

                list.Add(new WoDocumentLiteWithUrlDto(
                    d.WoDocumentId, d.WorkOrderId, d.FileName, d.Status, d.QrText,
                    d.ObjectKey, d.Description, d.CreatedByUserId, d.CreatedByUserName, d.CreatedAt, viewUrl));
            }

            var totalPages = (int)Math.Ceiling((double)res.TotalItems / pageSize);
            var meta = new PagedMeta(page, pageSize, res.TotalItems, totalPages);

            return Ok(ApiResponse<IReadOnlyList<WoDocumentLiteWithUrlDto>>.Ok(list, "OK", meta));
        }

        // ====== B) POST resolve-qr (tetap POST) ======
        // POST /api/v1/documents/resolve-qr
        // Body: { "QrText": "..." }

        [HttpPost("resolve-qr")]
        [Consumes("application/json")]
        public async Task<IActionResult> ResolveQr([FromBody] ApiResolveQrRequest body, CancellationToken ct = default)
        {
            if (body is null || string.IsNullOrWhiteSpace(body.QrText))
                return BadRequest(new ProblemDetails { Title = "QrText is required", Status = 400 });

            var doc = await _approvalQuery.GetWoDocumentByQrCode(body.QrText.Trim(), ct);
            if (doc is null)
                return NotFound(new ProblemDetails { Title = "WoDocument not found", Status = 404 });

            var viewUrl = await _docSvc.GetPresignedUrlAsync(doc.WoDocumentId, TimeSpan.FromMinutes(15));
            var dto = new WoDocumentLiteWithUrlDto(
                doc.WoDocumentId, doc.WorkOrderId, doc.FileName, doc.Status, doc.QrText,
                doc.ObjectKey, doc.Description, doc.CreatedByUserId, doc.CreatedByUserName, doc.CreatedAt, viewUrl);

            return Ok(ApiResponse<WoDocumentLiteWithUrlDto>.Ok(dto, "OK"));
        }
    }
}
