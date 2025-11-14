using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; // ⬅️ tambahkan
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

public sealed class ProcurementDocumentQuery : IProcurementDocumentQuery
{
    private readonly AppDbContext _db;
    // ⬇️ init supaya tidak pernah null, sekaligus hilangkan CS0649
    private readonly ILogger<ProcurementDocumentQuery> _logger = NullLogger<ProcurementDocumentQuery>.Instance;

    public ProcurementDocumentQuery(
        AppDbContext context,
        ILogger<ProcurementDocumentQuery> logger)
    {
        _db = context;
        _logger = logger; // DI akan override NullLogger dengan real logger
    }

    public async Task<ProcurementRequiredDocsDto?> GetRequiredDocsAsync(string procurementId, TimeSpan? _)
    {
        var procurement = await _db.Procurements
            .AsNoTracking()
            .Where(x => x.ProcurementId == procurementId)
            .Select(x => new { x.ProcurementId, x.JobTypeId })
            .FirstOrDefaultAsync();

        _logger.LogInformation("[ReqDocs] Procurement={Proc} => Found={Found} JobTypeId={JobTypeId}",
            procurementId, procurement != null, procurement?.JobTypeId);

        if (procurement is null) return null;

        var cfgCount = await _db.JobTypeDocuments
            .AsNoTracking()
            .Where(c => c.JobTypeId == procurement.JobTypeId)
            .CountAsync();
        _logger.LogInformation("[ReqDocs] JobTypeDocuments Count for JobTypeId={JobTypeId}: {Count}", procurement.JobTypeId, cfgCount);

        var docTypeIds = await _db.JobTypeDocuments
            .AsNoTracking()
            .Where(c => c.JobTypeId == procurement.JobTypeId)
            .Select(c => c.DocumentTypeId)
            .Distinct()
            .ToListAsync();

        var dtCount = await _db.DocumentTypes
            .AsNoTracking()
            .Where(dt => docTypeIds.Contains(dt.DocumentTypeId))
            .CountAsync();
        _logger.LogInformation("[ReqDocs] DocumentTypes referenced: {dtCount}", dtCount);

        var wdCount = await _db.ProcDocuments
            .AsNoTracking()
            .Where(d => d.ProcurementId == procurementId)
            .CountAsync();
        _logger.LogInformation("[ReqDocs] Existing ProcDocuments for Procurement={Procurement}: {Count}", procurementId, wdCount);

        var q =
            from cfg in _db.JobTypeDocuments.AsNoTracking()
            where cfg.JobTypeId == procurement.JobTypeId
            let lastDoc = _db.ProcDocuments.AsNoTracking()
                .Where(d => d.ProcurementId == procurementId && d.DocumentTypeId == cfg.DocumentTypeId)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault()
            orderby cfg.Sequence
            select new RequiredDocItemDto
            {
                JobTypeDocumentId = cfg.JobTypeDocumentId,
                Sequence = cfg.Sequence,
                DocumentTypeId = cfg.DocumentTypeId,
                DocumentTypeName = cfg.DocumentType.Name,
                IsMandatory = cfg.IsMandatory,
                IsUploadRequired = cfg.IsUploadRequired,
                IsGenerated = cfg.IsGenerated,
                RequiresApproval = cfg.RequiresApproval,
                Note = cfg.Note,
                Uploaded = lastDoc != null,
                ProcDocumentId = lastDoc != null ? lastDoc.ProcDocumentId : null,
                FileName = lastDoc != null ? lastDoc.FileName : null,
                Size = lastDoc != null ? lastDoc.Size : null,
                Status = lastDoc != null ? lastDoc.Status : null
            };

        _logger.LogDebug("[ReqDocs] SQL:\n{Sql}", q.ToQueryString());

        var items = await q.ToListAsync();
        _logger.LogInformation("[ReqDocs] Items materialized: {Count}", items.Count);

        return new ProcurementRequiredDocsDto
        {
            ProcurementId = procurement.ProcurementId,
            JobTypeId = procurement.JobTypeId!,
            Items = items
        };
    }
}
