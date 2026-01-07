using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class LdpRepository : ILdpRepository
    {
        private readonly AppDbContext _context;

        public LdpRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<(IReadOnlyList<LdpRecapDto> Items, int TotalCount)> GetAllAsync(
            int page,
            int pageSize,
            string? search = null,
            CancellationToken ct = default
        )
        {
            // Base query - start from Procurements
            var query = _context
                .Procurements.Include(p => p.JobType)
                .Include(p => p.PurchaseRequisition)
                .Include(p => p.ProcOffers)
                .Include(p => p.ProcDocuments!)
                .ThenInclude(pd => pd.DocumentType)
                .Include(p => p.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .AsNoTracking()
                .AsSplitQuery();

            // Apply search filter - focus on No WO and Job Name
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = $"%{search.Trim()}%";
                query = query.Where(p =>
                    (p.Wonum != null && EF.Functions.Like(p.Wonum, searchTerm))
                    || (p.JobName != null && EF.Functions.Like(p.JobName, searchTerm))
                    || (p.SpkNumber != null && EF.Functions.Like(p.SpkNumber, searchTerm))
                    || (p.ProcNum != null && EF.Functions.Like(p.ProcNum, searchTerm))
                );
            }

            // Order by StartDate descending (newest first)
            query = query.OrderByDescending(p => p.StartDate);

            // Get total count
            var totalCount = await query.CountAsync(ct);

            // Get paged data
            var procurements = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            // Get VendorRoundLetters for selected vendors
            var procurementIds = procurements.Select(p => p.ProcurementId).ToList();
            var vendorRoundLetters = await _context
                .VendorRoundLetters.Where(vrl => procurementIds.Contains(vrl.ProcurementId))
                .AsNoTracking()
                .ToListAsync(ct);

            // Map to DTO
            var items = procurements
                .Select(p =>
                {
                    var profitLoss = p.ProfitLosses.FirstOrDefault();
                    var selectedVendor = profitLoss?.SelectedVendor;
                    var pr = p.PurchaseRequisition;

                    // Get document dates by type name
                    var procDocs = p.ProcDocuments ?? new List<Core.Models.ProcDocuments>();
                    var rksDoc = procDocs.FirstOrDefault(d =>
                        d.DocumentType?.Name?.ToUpper().Contains("RKS") == true
                    );
                    var boqDoc = procDocs.FirstOrDefault(d =>
                        d.DocumentType?.Name?.ToUpper().Contains("BOQ") == true
                    );
                    var memoDoc = procDocs.FirstOrDefault(d =>
                        d.DocumentType?.Name?.ToUpper().Contains("MEMO") == true
                    );
                    var raDoc = procDocs.FirstOrDefault(d =>
                        d.DocumentType?.Name?.ToUpper().Contains("RA") == true
                    );

                    // Get vendor round letter for selected vendor
                    var vendorLetter =
                        selectedVendor != null
                            ? vendorRoundLetters.FirstOrDefault(vrl =>
                                vrl.ProcurementId == p.ProcurementId
                                && vrl.VendorId == selectedVendor.VendorId
                            )
                            : null;

                    // Combine ProcOffers items
                    var unitItems =
                        p.ProcOffers != null && p.ProcOffers.Any()
                            ? string.Join(", ", p.ProcOffers.Select(o => o.ItemPenawaran))
                            : null;

                    return new LdpRecapDto
                    {
                        ProcurementId = p.ProcurementId,

                        // Basic Info
                        NoWo = p.Wonum,
                        NoSpk = p.SpkNumber,
                        NamaVendor = selectedVendor?.VendorName,
                        TglMulai = p.StartDate,
                        TglSelesai = p.EndDate,
                        TextPekerjaan = p.JobName,
                        JobType = p.JobType?.TypeName,
                        ContractType = p.ContractType.ToString(),
                        LtcName = p.LtcName,

                        // PR Related - use PR description as text pekerjaan PR
                        TextPekerjaanPr = pr?.Description,

                        // Additional Info - fields not available, will show "-"
                        Flag50K = null,
                        NoAccrual = null,
                        YearJob = p.StartDate.Year,
                        NoRig = null,

                        // Financial
                        NilaiPnl = profitLoss?.Profit,
                        NilaiAccrual = profitLoss?.AccrualAmount,
                        NilaiRealisasi = profitLoss?.RealizationAmount,

                        // Document Numbers
                        NoSpmp = p.SpmpNumber,
                        NoHte = null,
                        ProjectRegion = p.ProjectRegion.ToString(),
                        ProjectCode = p.ProjectCode,
                        LinkDokumen = procDocs.FirstOrDefault()?.ObjectKey,

                        // Items
                        UnitItemPenawaran = unitItems,
                        SuratPenawaranVendor = vendorLetter?.LetterNumber,
                        Memorandum = p.MemoNumber,
                        TglDoc = p.DocumentDate,

                        // Keterangan - fields not available
                        Keterangan1 = p.Note,
                        PotensiAccrual = null,
                        TglAccrual = p.PotentialAccrualDate,
                        StatusAccrual = null,

                        // Document Dates - CreatedAt as start date, no end date available
                        RksTglMulai = rksDoc?.CreatedAt,
                        RksTglSelesai = null,
                        BoqTglMulai = boqDoc?.CreatedAt,
                        BoqTglSelesai = null,
                        MemoTglMulai = memoDoc?.CreatedAt,
                        MemoTglSelesai = null,
                        RaTglMulai = raDoc?.CreatedAt,
                        RaTglSelesai = null,

                        // Purchase Requisition
                        NoPr = pr?.PrNumber,
                        TanggalBuatPr = pr?.CreatedAt,
                        TanggalRilisPr = null,

                        // Approval Dates - not available in current model
                        TanggalApprovalOps = null,
                        TanggalApprovalManager = null,
                        TanggalApprovalVp = null,
                        TanggalApprovalDirektur = null,
                        TanggalSubmitIspa = pr?.IspaSubmittedAt,
                    };
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<int> CountAsync(CancellationToken ct = default)
        {
            return await _context.Procurements.CountAsync(ct);
        }
    }
}
