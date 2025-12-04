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

    public ProcurementDocumentQuery(AppDbContext context)
    {
        _db = context;
    }

    public async Task<ProcurementRequiredDocsDto?> GetRequiredDocsAsync(
        string procurementId,
        TimeSpan? _
    )
    {
        var procurement = await _db
            .Procurements.AsNoTracking()
            .Where(x => x.ProcurementId == procurementId)
            .Select(x => new { x.ProcurementId, x.JobTypeId, x.ProcurementCategory })
            .FirstOrDefaultAsync();

        if (procurement is null)
            return null;

        var cfgCount = await _db
            .JobTypeDocuments.AsNoTracking()
            .Where(c => c.JobTypeId == procurement.JobTypeId)
            .CountAsync();

        var docTypeIds = await _db
            .JobTypeDocuments.AsNoTracking()
            .Where(c => c.JobTypeId == procurement.JobTypeId)
            .Select(c => c.DocumentTypeId)
            .Distinct()
            .ToListAsync();

        var dtCount = await _db
            .DocumentTypes.AsNoTracking()
            .Where(dt => docTypeIds.Contains(dt.DocumentTypeId))
            .CountAsync();

        var wdCount = await _db
            .ProcDocuments.AsNoTracking()
            .Where(d => d.ProcurementId == procurementId)
            .CountAsync();

        var latestPnl = await _db
            .ProfitLosses.AsNoTracking()
            .Where(p => p.ProcurementId == procurementId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        // Jika PNL ada, hitung threshold justifikasi berdasarkan final offer (atau kalkulasi dari vendor offers).
        decimal bestFinalOffer = 0m;
        if (latestPnl != null)
        {
            bestFinalOffer = latestPnl.SelectedVendorFinalOffer;

            if (bestFinalOffer <= 0m)
            {
                var offers = await _db
                    .VendorOffers.AsNoTracking()
                    .Where(o => o.ProfitLossId == latestPnl.ProfitLossId)
                    .ToListAsync();

                if (offers.Count > 0)
                {
                    bestFinalOffer = offers
                        .GroupBy(o => o.VendorId)
                        .Select(group =>
                        {
                            // ambil harga last round per item, lalu jumlahkan (qty * trip * price)
                            var perItem = group
                                .GroupBy(x => x.ProcOfferId)
                                .Select(gg =>
                                {
                                    var last = gg.OrderBy(x => x.Round).Last();
                                    return last.Price * last.Quantity * last.Trip;
                                });

                            return perItem.Sum();
                        })
                        .DefaultIfEmpty(0m)
                        .Min();
                }
            }
        }

        var needJustifikasi = bestFinalOffer > 300_000_000m;

        var q =
            from cfg in _db.JobTypeDocuments.AsNoTracking()
            where cfg.JobTypeId == procurement.JobTypeId
                && (cfg.ProcurementCategory == null || cfg.ProcurementCategory == procurement.ProcurementCategory)
                && (cfg.DocumentType.Name != "Justifikasi" || needJustifikasi)
            let lastDoc = _db
                .ProcDocuments.AsNoTracking()
                .Where(d =>
                    d.ProcurementId == procurementId && d.DocumentTypeId == cfg.DocumentTypeId
                )
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
                Status = lastDoc != null ? lastDoc.Status : null,
            };

        var items = await q.ToListAsync();

        return new ProcurementRequiredDocsDto
        {
            ProcurementId = procurement.ProcurementId,
            JobTypeId = procurement.JobTypeId!,
            Items = items,
        };
    }
}
