using Amazon.Runtime.Internal.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

public class WorkOrderDocumentQuery(AppDbContext db) : IWorkOrderDocumentQuery
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<WorkOrderDocumentQuery> _logger = NullLogger<WorkOrderDocumentQuery>.Instance;

    public async Task<WorkOrderRequiredDocsDto?> GetRequiredDocsAsync(string workOrderId, TimeSpan? _)
    {
        // 1) WorkOrder
        var wo = await _db.WorkOrders
            .AsNoTracking()
            .Where(x => x.WorkOrderId == workOrderId)
            .Select(x => new { x.WorkOrderId, x.WoTypeId })
            .FirstOrDefaultAsync();

        _logger?.LogInformation("[ReqDocs] WO={WO} => Found={Found} WoTypeId={WoTypeId}",
            workOrderId, wo != null, wo?.WoTypeId);

        if (wo is null) return null;

        // 2) Ada konfigurasi WoTypeDocuments untuk WoTypeId ini?
        var cfgCount = await _db.WoTypesDocuments
            .AsNoTracking()
            .Where(c => c.WoTypeId == wo.WoTypeId)
            .CountAsync();
        _logger?.LogInformation("[ReqDocs] WoTypeDocuments Count for WoTypeId={WoTypeId}: {Count}", wo.WoTypeId, cfgCount);

        // 3) Ada DocumentTypes yang direferensikan oleh WoTypeDocuments tsb?
        var docTypeIds = await _db.WoTypesDocuments
            .AsNoTracking()
            .Where(c => c.WoTypeId == wo.WoTypeId)
            .Select(c => c.DocumentTypeId)
            .Distinct()
            .ToListAsync();

        var dtCount = await _db.DocumentTypes
            .AsNoTracking()
            .Where(dt => docTypeIds.Contains(dt.DocumentTypeId))
            .CountAsync();
        _logger?.LogInformation("[ReqDocs] DocumentTypes referenced: {dtCount}", dtCount);

        // 4) Ada WoDocuments untuk WO ini?
        var wdCount = await _db.WoDocuments
            .AsNoTracking()
            .Where(d => d.WorkOrderId == workOrderId)
            .CountAsync();
        _logger?.LogInformation("[ReqDocs] Existing WoDocuments for WO={WO}: {wdCount}", workOrderId, wdCount);

        // 5) Query utama (pakai OUTER APPLY/let lastDoc)
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
                DocumentTypeName = cfg.DocumentType.Name,
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

        _logger?.LogDebug("[ReqDocs] SQL:\n{Sql}", q.ToQueryString());

        var items = await q.ToListAsync();
        _logger?.LogInformation("[ReqDocs] Items materialized: {Count}", items.Count);

        return new WorkOrderRequiredDocsDto
        {
            WorkOrderId = wo.WorkOrderId,
            WoTypeId = wo.WoTypeId!,
            Items = items
        };
    }

}
