using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

public class WorkOrderDocumentQuery(AppDbContext db, ILogger<WorkOrderDocumentQuery>? logger = null)
    : IWorkOrderDocumentQuery
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<WorkOrderDocumentQuery> _logger =
        logger ?? NullLogger<WorkOrderDocumentQuery>.Instance;

    public async Task<WorkOrderRequiredDocsDto?> GetRequiredDocsAsync(string workOrderId, TimeSpan? timeout)
    {
        using var cts = timeout.HasValue
            ? new CancellationTokenSource(timeout.Value)
            : new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var ct = cts.Token;

        // 1) WorkOrder
        var wo = await _db.WorkOrders
            .AsNoTracking()
            .Where(x => x.WorkOrderId == workOrderId)
            .Select(x => new { x.WorkOrderId, x.WoTypeId })
            .FirstOrDefaultAsync(ct);

        _logger.LogInformation("[ReqDocs] WO={WO} => Found={Found} WoTypeId={WoTypeId}",
            workOrderId, wo != null, wo?.WoTypeId);

        if (wo is null) return null;

        // 2) Ada konfigurasi WoTypeDocuments untuk WoTypeId ini?
        var cfgCount = await _db.WoTypesDocuments
            .AsNoTracking()
            .Where(c => c.WoTypeId == wo.WoTypeId)
            .CountAsync(ct);
        _logger.LogInformation("[ReqDocs] WoTypeDocuments Count for WoTypeId={WoTypeId}: {Count}", wo.WoTypeId, cfgCount);

        // 3) DocumentTypes yang direferensikan
        var docTypeIds = await _db.WoTypesDocuments
            .AsNoTracking()
            .Where(c => c.WoTypeId == wo.WoTypeId)
            .Select(c => c.DocumentTypeId)
            .Distinct()
            .ToListAsync(ct);

        var dtCount = await _db.DocumentTypes
            .AsNoTracking()
            .Where(dt => docTypeIds.Contains(dt.DocumentTypeId))
            .CountAsync(ct);
        _logger.LogInformation("[ReqDocs] DocumentTypes referenced: {Count}", dtCount);

        // 4) WoDocuments yang sudah ada
        var wdCount = await _db.WoDocuments
            .AsNoTracking()
            .Where(d => d.WorkOrderId == workOrderId)
            .CountAsync(ct);
        _logger.LogInformation("[ReqDocs] Existing WoDocuments for WO={WO}: {Count}", workOrderId, wdCount);

        // 5) Query utama
        var q =
            from cfg in _db.WoTypesDocuments.AsNoTracking()
            where cfg.WoTypeId == wo.WoTypeId
            let lastDoc = _db.WoDocuments.AsNoTracking()
                .Where(d => d.WorkOrderId == workOrderId && d.DocumentTypeId == cfg.DocumentTypeId)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault()
            orderby cfg.Sequence
            select new RequiredDocItemDto
            {
                WoTypeDocumentId = cfg.WoTypeDocumentId,
                Sequence = cfg.Sequence,
                DocumentTypeId = cfg.DocumentTypeId,
                DocumentTypeName = cfg.DocumentType.Name, // asumsi relasi wajib
                IsMandatory = cfg.IsMandatory,
                IsUploadRequired = cfg.IsUploadRequired,
                IsGenerated = cfg.IsGenerated,
                RequiresApproval = cfg.RequiresApproval,
                Note = cfg.Note,
                Uploaded = lastDoc != null,
                WoDocumentId = lastDoc != null ? lastDoc.WoDocumentId : null,
                FileName = lastDoc != null ? lastDoc.FileName : null,
                Size = lastDoc != null ? lastDoc.Size : null,
                Status = lastDoc != null ? lastDoc.Status : null
            };

        _logger.LogDebug("[ReqDocs] SQL:\n{Sql}", q.ToQueryString());

        var items = await q.ToListAsync(ct);
        _logger.LogInformation("[ReqDocs] Items materialized: {Count}", items.Count);

        return new WorkOrderRequiredDocsDto
        {
            WorkOrderId = wo.WorkOrderId,
            WoTypeId = wo.WoTypeId!,
            Items = items
        };
    }
}
