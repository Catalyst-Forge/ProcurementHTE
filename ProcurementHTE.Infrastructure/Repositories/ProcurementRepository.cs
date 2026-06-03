using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Utils;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class ProcurementRepository : IProcurementRepository
    {
        private readonly AppDbContext _context;
        private const string PROC_PREFIX = "PROC";
        private const int MAX_RETRY_ATTEMPTS = 5;

        public ProcurementRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        #region Query Methods

        public async Task<Core.Common.PagedResult<Procurement>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct,
            string? userId
        )
        {
            var query = BuildBaseQuery();

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
                query = ApplySearchFilter(query, search.Trim(), fields);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(proc => proc.UserId == userId);
            }

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(w => w.CreatedAt),
                ct: ct
            );
        }

        public async Task<Procurement?> GetByIdAsync(string id)
        {
            return await _context
                .Procurements.Include(procurement => procurement.ProcOffers)
                .Include(procurement => procurement.Status)
                .Include(procurement => procurement.JobType)
                .Include(procurement => procurement.User)
                .Include(procurement => procurement.ProcDetails)
                .Include(procurement => procurement.ProfitLosses)
                    .ThenInclude(pl => pl.SelectedVendor)
                .Include(procurement => procurement.AppoUser)
                .Include(procurement => procurement.ApInvoiceUser)
                .Include(procurement => procurement.ArUser)
                .FirstOrDefaultAsync(procurement => procurement.ProcurementId == id);
        }

        public async Task<Procurement?> GetWithSelectedOfferAsync(string id)
        {
            return await _context
                .Procurements.Include(procurement => procurement.VendorOffers)
                .ThenInclude(vendorOffer => vendorOffer.Vendor)
                .FirstOrDefaultAsync(procurement => procurement.ProcurementId == id);
        }

        public async Task<IReadOnlyList<Procurement>> GetRecentByUserAsync(
            string userId,
            int limit,
            CancellationToken ct
        )
        {
            return await _context
                .Procurements.Where(procurement => procurement.UserId == userId)
                .OrderByDescending(procurement => procurement.CreatedAt)
                .Include(procurement => procurement.Status)
                .AsNoTracking()
                .Take(limit)
                .ToListAsync(ct);
        }

        public Task<int> CountAsync(CancellationToken ct)
        {
            return _context.Procurements.CountAsync(ct);
        }

        public async Task<IReadOnlyList<ProcurementStatusCountDto>> GetCountByStatusAsync()
        {
            return await _context
                .Procurements.GroupBy(procurement => procurement.Status!.StatusName)
                .Select(group => new ProcurementStatusCountDto
                {
                    Status = group.Key,
                    Count = group.Count(),
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Procurement>> GetAllForSelectionAsync()
        {
            return await _context
                .Procurements.Include(procurement => procurement.JobType)
                .Include(procurement => procurement.Status)
                .Include(procurement => procurement.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .AsSplitQuery()
                .AsNoTracking()
                .OrderByDescending(procurement => procurement.CreatedAt)
                .ToListAsync();
        }

        public async Task<Core.Common.PagedResult<Procurement>> GetProcurementsForAppoApprovalAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        )
        {
            var query = BuildBaseQuery()
                .Where(p =>
                    p.Status != null && p.Status.StatusName == "Waiting Pickup"
                );

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
                query = ApplySearchFilter(query, search.Trim(), fields);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(w => w.CreatedAt),
                ct: ct
            );
        }

        public async Task<Core.Common.PagedResult<Procurement>> GetMyAppoPickupsAsync(
            string appoUserId,
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        )
        {
            var query = BuildBaseQuery()
                .Include(p => p.AppoUser)
                .Where(p => p.AppoUserId == appoUserId);

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
                query = ApplySearchFilter(query, search.Trim(), fields);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(w => w.CreatedAt),
                ct: ct
            );
        }

        #endregion

        #region Lookup Methods

        public async Task<Status?> GetStatusByNameAsync(string name)
        {
            var normalized = name.Trim();
            return await _context
                .Statuses.AsNoTracking()
                .FirstOrDefaultAsync(status =>
                    status.StatusName != null && EF.Functions.Like(status.StatusName, normalized)
                );
        }

        public Task<List<Status>> GetStatusesAsync()
        {
            return _context
                .Statuses.AsNoTracking()
                .OrderBy(status => status.StatusName)
                .ToListAsync();
        }

        public Task<JobTypes?> GetJobTypeByIdAsync(string id)
        {
            return _context.JobTypes.FirstOrDefaultAsync(job => job.JobTypeId == id);
        }

        public Task<List<JobTypes>> GetJobTypesAsync()
        {
            return _context.JobTypes.OrderBy(job => job.TypeName).ToListAsync();
        }

        #endregion

        #region Command Methods

        public async Task StoreProcurementWithDetailsAsync(
            Procurement procurement,
            List<ProcDetail> details,
            List<ProcOffer> offers
        )
        {
            var validDetails = FilterValidDetails(details);
            var validOffers = FilterValidOffers(offers);

            for (var attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                using var transactionDb = await _context.Database.BeginTransactionAsync();

                try
                {
                    procurement.ProcNum = await GenerateNextProcNumAsync();

                    AssignProcurementIdToDetails(procurement.ProcurementId, validDetails);
                    AssignProcurementIdToOffers(procurement.ProcurementId, validOffers);

                    await _context.Procurements.AddAsync(procurement);

                    if (validDetails.Count != 0)
                        await _context.ProcDetails.AddRangeAsync(validDetails);

                    if (validOffers.Count != 0)
                        await _context.ProcOffers.AddRangeAsync(validOffers);

                    await _context.SaveChangesAsync();
                    await transactionDb.CommitAsync();
                    return;
                }
                catch (DbUpdateException ex) when (IsUniqueProcNumViolation(ex))
                {
                    await transactionDb.RollbackAsync();
                    if (attempt == MAX_RETRY_ATTEMPTS)
                    {
                        throw new InvalidOperationException(
                            "Gagal membuat nomor Procurement unik setelah beberapa percobaan.",
                            ex
                        );
                    }
                }
                catch
                {
                    await transactionDb.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task UpdateProcurementAsync(Procurement procurement)
        {
            try
            {
                _context.Entry(procurement).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context
                    .Procurements.AsNoTracking()
                    .AnyAsync(procurement =>
                        procurement.ProcurementId == procurement.ProcurementId
                    );

                if (!exists)
                    throw new KeyNotFoundException(
                        $"Procurement dengan ID {procurement.ProcurementId} tidak ditemukan"
                    );

                throw new InvalidOperationException(
                    "Data telah diubah oleh user lain. Silakan refresh dan coba lagi"
                );
            }
        }

        public async Task UpdateProcurementWithDetailsAsync(
            Procurement procurement,
            List<ProcDetail> details,
            List<ProcOffer> offers
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Entry(procurement).State = EntityState.Modified;

                var existingDetails = await _context
                    .ProcDetails.Where(detail => detail.ProcurementId == procurement.ProcurementId)
                    .ToListAsync();

                var existingOffers = await _context
                    .ProcOffers.Where(offer => offer.ProcurementId == procurement.ProcurementId)
                    .ToListAsync();

                // Always delete and recreate details (no ProfitLoss dependency)
                if (existingDetails.Count != 0)
                    _context.ProcDetails.RemoveRange(existingDetails);

                var validDetails = FilterValidDetails(details);
                AssignProcurementIdToDetails(procurement.ProcurementId, validDetails);

                if (validDetails.Count != 0)
                    await _context.ProcDetails.AddRangeAsync(validDetails);

                // SMART UPDATE FOR OFFERS: Update existing, add new, delete removed
                // This prevents cascade deletion of ProfitLossItems
                var validOffers = FilterValidOffers(offers);
                AssignProcurementIdToOffers(procurement.ProcurementId, validOffers);

                // Match offers by position (order) - this is the simplest and most reliable approach
                for (int i = 0; i < validOffers.Count; i++)
                {
                    if (i < existingOffers.Count)
                    {
                        // Update existing offer in place (preserves ProcOfferId)
                        var existing = existingOffers[i];
                        existing.ItemPenawaran = validOffers[i].ItemPenawaran;
                        existing.Qty = validOffers[i].Qty;
                        existing.Unit = validOffers[i].Unit;
                        existing.UnitRevenue = validOffers[i].UnitRevenue;
                        _context.Entry(existing).State = EntityState.Modified;
                    }
                    else
                    {
                        // Add new offer (new item added)
                        await _context.ProcOffers.AddAsync(validOffers[i]);
                    }
                }

                // Remove offers that were deleted (if new list is shorter)
                if (existingOffers.Count > validOffers.Count)
                {
                    var offersToRemove = existingOffers.Skip(validOffers.Count).ToList();
                    _context.ProcOffers.RemoveRange(offersToRemove);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(Procurement procurement, string deletedByUserId)
        {
            var entityToDelete = await _context.Procurements.FirstOrDefaultAsync(
                p => p.ProcurementId == procurement.ProcurementId
            );

            if (entityToDelete != null)
            {
                entityToDelete.IsDeleted = true;
                entityToDelete.DeletedAt = DateTime.UtcNow;
                entityToDelete.DeletedBy = deletedByUserId;
                
                // Prepend dash to ProcNum to allow reuse of the same number
                if (!string.IsNullOrEmpty(entityToDelete.ProcNum))
                {
                    entityToDelete.ProcNum = $"-{entityToDelete.ProcNum}";
                }
                
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Private Helper Methods

        private IQueryable<Procurement> BuildBaseQuery()
        {
            return _context
                .Procurements.Include(procurement => procurement.ProcOffers)
                .Include(procurement => procurement.Status)
                .Include(procurement => procurement.JobType)
                .Include(procurement => procurement.User)
                .Include(procurement => procurement.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .AsSplitQuery()
                .AsNoTracking();
        }

        private static IQueryable<Procurement> ApplySearchFilter(
            IQueryable<Procurement> query,
            string searchTerm,
            ISet<string> fields
        )
        {
            bool byProcNum = fields.Contains("ProcNum", StringComparer.OrdinalIgnoreCase);
            bool byWonum = fields.Contains("Wonum", StringComparer.OrdinalIgnoreCase);
            bool byJobName = fields.Contains("JobName", StringComparer.OrdinalIgnoreCase);
            bool byProjectCode = fields.Contains("ProjectCode", StringComparer.OrdinalIgnoreCase);
            bool bySelectedVendor = fields.Contains(
                "SelectedVendorName",
                StringComparer.OrdinalIgnoreCase
            );
            bool byStatus = fields.Contains("Status", StringComparer.OrdinalIgnoreCase);

            var like = $"%{searchTerm}%";

            return query.Where(procurement =>
                (
                    byProcNum
                    && procurement.ProcNum != null
                    && EF.Functions.Like(procurement.ProcNum, like)
                )
                || (
                    byWonum
                    && procurement.Wonum != null
                    && EF.Functions.Like(procurement.Wonum, like)
                )
                || (
                    byJobName
                    && procurement.JobName != null
                    && EF.Functions.Like(procurement.JobName, like)
                )
                || (
                    byProjectCode
                    && procurement.ProjectCode != null
                    && EF.Functions.Like(procurement.ProjectCode, like)
                )
                || (
                    byStatus
                    && procurement.Status != null
                    && EF.Functions.Like(procurement.Status.StatusName, like)
                )
            );
        }

        private async Task<string> GenerateNextProcNumAsync()
        {
            var lastProcNum = await GetLastProcNumAsync(PROC_PREFIX);
            return SequenceNumberGenerator.NumId(PROC_PREFIX, lastProcNum);
        }

        private async Task<string?> GetLastProcNumAsync(string prefix)
        {
            return await _context
                .Procurements.Where(procurement => procurement.ProcNum!.StartsWith(prefix))
                .OrderByDescending(procurement => procurement.ProcNum)
                .Select(procurement => procurement.ProcNum)
                .FirstOrDefaultAsync();
        }

        private static List<ProcDetail> FilterValidDetails(List<ProcDetail>? details)
        {
            return (details ?? [])
                .Where(detail =>
                    !string.IsNullOrWhiteSpace(detail.ItemName)
                    && detail.Quantity.HasValue
                    && detail.Quantity.Value > 0
                )
                .ToList();
        }

        private static List<ProcOffer> FilterValidOffers(List<ProcOffer>? offers)
        {
            return (offers ?? [])
                .Where(offer => !string.IsNullOrWhiteSpace(offer.ItemPenawaran))
                .ToList();
        }

        private static void AssignProcurementIdToDetails(
            string procurementId,
            List<ProcDetail> details
        )
        {
            foreach (var detail in details)
                detail.ProcurementId = procurementId;
        }

        private static void AssignProcurementIdToOffers(
            string procurementId,
            List<ProcOffer> offers
        )
        {
            foreach (var offer in offers)
                offer.ProcurementId = procurementId;
        }

        private static bool IsUniqueProcNumViolation(DbUpdateException ex) =>
            ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
            && sqlEx.Number == 2627
            && (sqlEx.Message?.Contains("AK_Procurements_ProcNum") ?? false);

        #endregion

        #region Accrual Methods

        public async Task<Core.Common.PagedResult<Procurement>> GetProcurementsForAccrualAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        )
        {
            // Status yang diizinkan untuk muncul di Accrual (setelah Waiting Pickup)
            var allowedStatuses = new[] { "Waiting Pickup", "In Progress", "Completed", "Closed" };

            var query = _context.Procurements
                .Include(p => p.JobType)
                .Include(p => p.Status)
                .Include(p => p.ProfitLosses)
                    .ThenInclude(pl => pl.SelectedVendor)
                .Include(p => p.AccrualFilledByUser)
                .AsNoTracking();

            // Filter: hanya procurement yang sudah punya SPMP (setelah Operation selesai)
            // DAN status minimal "Waiting Pickup"
            query = query.Where(p => 
                !string.IsNullOrEmpty(p.SpmpNumber) &&
                p.Status != null && 
                allowedStatuses.Contains(p.Status.StatusName));

            // Filter berdasarkan status accrual
            if (!string.IsNullOrEmpty(filter))
            {
                query = filter.ToLower() switch
                {
                    "pending" => query.Where(p => string.IsNullOrEmpty(p.NoAccrual)),
                    "filled" => query.Where(p => !string.IsNullOrEmpty(p.NoAccrual)),
                    _ => query
                };
            }

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = $"%{search.Trim()}%";
                query = query.Where(p =>
                    (p.Wonum != null && EF.Functions.Like(p.Wonum, searchTerm))
                    || (p.JobName != null && EF.Functions.Like(p.JobName, searchTerm))
                    || (p.SpkNumber != null && EF.Functions.Like(p.SpkNumber, searchTerm))
                    || (p.ProcNum != null && EF.Functions.Like(p.ProcNum, searchTerm))
                    || (p.NoAccrual != null && EF.Functions.Like(p.NoAccrual, searchTerm))
                );
            }

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(p => p.CreatedAt),
                ct: ct
            );
        }

        public async Task<Core.Common.PagedResult<Procurement>> GetProcurementsForApInvoiceAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        )
        {
            var query = _context.Procurements
                .Include(p => p.JobType)
                .Include(p => p.Status)
                .Include(p => p.AppoUser)
                .Include(p => p.ApInvoiceUser)
                .Include(p => p.ProfitLosses)
                    .ThenInclude(pl => pl.SelectedVendor)
                .AsNoTracking();

            // Filter: procurement yang sudah di-pickup oleh AP-PO DAN sudah Done PO
            query = query.Where(p =>
                !string.IsNullOrEmpty(p.AppoUserId) &&
                p.ProcurementStatus == ProcurementStatus.DonePO);

            // Filter berdasarkan status AP-Invoice pickup
            if (!string.IsNullOrEmpty(filter))
            {
                query = filter.ToLower() switch
                {
                    "pending" => query.Where(p => string.IsNullOrEmpty(p.ApInvoiceUserId)),
                    "filled" => query.Where(p => !string.IsNullOrEmpty(p.ApInvoiceUserId)),
                    _ => query
                };
            }

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = $"%{search.Trim()}%";
                query = query.Where(p =>
                    (p.Wonum != null && EF.Functions.Like(p.Wonum, searchTerm))
                    || (p.JobName != null && EF.Functions.Like(p.JobName, searchTerm))
                    || (p.SpkNumber != null && EF.Functions.Like(p.SpkNumber, searchTerm))
                    || (p.ProcNum != null && EF.Functions.Like(p.ProcNum, searchTerm))
                    || (p.SANo != null && EF.Functions.Like(p.SANo, searchTerm))
                    || (p.SP3No != null && EF.Functions.Like(p.SP3No, searchTerm))
                );
            }

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(p => p.CreatedAt),
                ct: ct
            );
        }

        public async Task<Core.Common.PagedResult<Procurement>> GetMyApInvoicePickupsAsync(
            string apInvoiceUserId,
            int page,
            int pageSize,
            string? search,
            CancellationToken ct
        )
        {
            var query = _context.Procurements
                .Include(p => p.JobType)
                .Include(p => p.Status)
                .Include(p => p.AppoUser)
                .Include(p => p.ApInvoiceUser)
                .Include(p => p.ProfitLosses)
                    .ThenInclude(pl => pl.SelectedVendor)
                .AsNoTracking()
                .Where(p => p.ApInvoiceUserId == apInvoiceUserId);

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = $"%{search.Trim()}%";
                query = query.Where(p =>
                    (p.Wonum != null && EF.Functions.Like(p.Wonum, searchTerm))
                    || (p.JobName != null && EF.Functions.Like(p.JobName, searchTerm))
                    || (p.SpkNumber != null && EF.Functions.Like(p.SpkNumber, searchTerm))
                    || (p.ProcNum != null && EF.Functions.Like(p.ProcNum, searchTerm))
                    || (p.SANo != null && EF.Functions.Like(p.SANo, searchTerm))
                    || (p.SP3No != null && EF.Functions.Like(p.SP3No, searchTerm))
                );
            }

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(p => p.ApInvoicePickedUpAt ?? p.CreatedAt),
                ct: ct
            );
        }

        public async Task<Core.Common.PagedResult<Procurement>> GetProcurementsForArPickupAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        )
        {
            // Status yang diizinkan untuk AR pickup (setelah publish)
            var allowedStatuses = new[] { "Waiting Pickup", "In Progress", "Completed", "Closed" };

            var query = _context.Procurements
                .Include(p => p.JobType)
                .Include(p => p.Status)
                .Include(p => p.AppoUser)
                .Include(p => p.ArUser)
                .Include(p => p.AccrualFilledByUser)
                .Include(p => p.ProfitLosses)
                    .ThenInclude(pl => pl.SelectedVendor)
                .AsNoTracking();

            // Filter: procurement yang sudah publish (status minimal "Waiting Pickup")
            // AR bisa pickup secara paralel dengan AP-PO setelah publish
            query = query.Where(p => 
                p.Status != null && 
                allowedStatuses.Contains(p.Status.StatusName));

            // Filter berdasarkan status AR pickup
            if (!string.IsNullOrEmpty(filter))
            {
                query = filter.ToLower() switch
                {
                    "pending" => query.Where(p => string.IsNullOrEmpty(p.ArUserId)),
                    "picked" => query.Where(p => !string.IsNullOrEmpty(p.ArUserId)),
                    _ => query
                };
            }

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = $"%{search.Trim()}%";
                query = query.Where(p =>
                    (p.Wonum != null && EF.Functions.Like(p.Wonum, searchTerm))
                    || (p.JobName != null && EF.Functions.Like(p.JobName, searchTerm))
                    || (p.SpkNumber != null && EF.Functions.Like(p.SpkNumber, searchTerm))
                    || (p.ProcNum != null && EF.Functions.Like(p.ProcNum, searchTerm))
                    || (p.NoAccrual != null && EF.Functions.Like(p.NoAccrual, searchTerm))
                );
            }

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(p => p.CreatedAt),
                ct: ct
            );
        }

        public async Task<Core.Common.PagedResult<Procurement>> GetMyArPickupsAsync(
            string arUserId,
            int page,
            int pageSize,
            string? search,
            CancellationToken ct
        )
        {
            var query = _context.Procurements
                .Include(p => p.JobType)
                .Include(p => p.Status)
                .Include(p => p.AppoUser)
                .Include(p => p.ArUser)
                .Include(p => p.AccrualFilledByUser)
                .Include(p => p.ProfitLosses)
                    .ThenInclude(pl => pl.SelectedVendor)
                .AsNoTracking()
                .Where(p => p.ArUserId == arUserId);

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = $"%{search.Trim()}%";
                query = query.Where(p =>
                    (p.Wonum != null && EF.Functions.Like(p.Wonum, searchTerm))
                    || (p.JobName != null && EF.Functions.Like(p.JobName, searchTerm))
                    || (p.SpkNumber != null && EF.Functions.Like(p.SpkNumber, searchTerm))
                    || (p.ProcNum != null && EF.Functions.Like(p.ProcNum, searchTerm))
                    || (p.NoAccrual != null && EF.Functions.Like(p.NoAccrual, searchTerm))
                );
            }

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(p => p.CreatedAt),
                ct: ct
            );
        }

        #endregion

        #region Procurement Tracking Methods

        public async Task<Procurement?> GetWithTrackingDataAsync(string procurementId, CancellationToken ct = default)
        {
            return await _context.Procurements
                .Include(p => p.JobType)
                    .ThenInclude(jt => jt!.JobTypeDocuments)
                    .ThenInclude(jtd => jtd.DocumentType)
                .Include(p => p.Status)
                .Include(p => p.PurchaseRequisition)
                .Include(p => p.IspaSubmittedByUser)
                .Include(p => p.PoSubmittedByUser)
                .Include(p => p.HardcopySubmittedByUser)
                .Include(p => p.RejectedByUser)
                .Include(p => p.ApprovalSentByUser)
                .Include(p => p.AnalystHteUser)
                .Include(p => p.AssistantManagerUser)
                .Include(p => p.ManagerUser)
                .Include(p => p.StatusHistories.OrderBy(h => h.ChangedAt))
                    .ThenInclude(h => h.ChangedByUser)
                .Include(p => p.ProcDocuments!)
                    .ThenInclude(pd => pd.DocumentType)
                .Include(p => p.ProfitLosses)
                    .ThenInclude(pl => pl.SelectedVendor)
                .Include(p => p.ProfitLosses)
                    .ThenInclude(pl => pl.Items)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.ProcurementId == procurementId, ct);
        }

        public async Task<Procurement?> GetByProcNumWithTrackingAsync(string procNum, CancellationToken ct = default)
        {
            // Search by ProcNum OR Wonum (Work Order Number)
            return await _context.Procurements
                .Include(p => p.JobType)
                    .ThenInclude(jt => jt!.JobTypeDocuments)
                    .ThenInclude(jtd => jtd.DocumentType)
                .Include(p => p.Status)
                .Include(p => p.PurchaseRequisition)
                .Include(p => p.IspaSubmittedByUser)
                .Include(p => p.PoSubmittedByUser)
                .Include(p => p.HardcopySubmittedByUser)
                .Include(p => p.RejectedByUser)
                .Include(p => p.ApprovalSentByUser)
                .Include(p => p.AnalystHteUser)
                .Include(p => p.AssistantManagerUser)
                .Include(p => p.ManagerUser)
                .Include(p => p.StatusHistories.OrderBy(h => h.ChangedAt))
                    .ThenInclude(h => h.ChangedByUser)
                .Include(p => p.ProcDocuments!)
                    .ThenInclude(pd => pd.DocumentType)
                .Include(p => p.ProfitLosses)
                    .ThenInclude(pl => pl.SelectedVendor)
                .Include(p => p.ProfitLosses)
                    .ThenInclude(pl => pl.Items)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.ProcNum == procNum || p.Wonum == procNum || p.SpmpNumber == procNum, ct);
        }

        public async Task<IReadOnlyList<Procurement>> GetByPrIdWithTrackingAsync(string prId, CancellationToken ct = default)
        {
            return await _context.Procurements
                .Where(p => p.PrId == prId)
                .Include(p => p.JobType)
                    .ThenInclude(jt => jt!.JobTypeDocuments)
                    .ThenInclude(jtd => jtd.DocumentType)
                .Include(p => p.Status)
                .Include(p => p.IspaSubmittedByUser)
                .Include(p => p.PoSubmittedByUser)
                .Include(p => p.HardcopySubmittedByUser)
                .Include(p => p.RejectedByUser)
                .Include(p => p.ApprovalSentByUser)
                .Include(p => p.StatusHistories.OrderBy(h => h.ChangedAt))
                    .ThenInclude(h => h.ChangedByUser)
                .Include(p => p.ProcDocuments!)
                    .ThenInclude(pd => pd.DocumentType)
                .AsNoTracking()
                .AsSplitQuery()
                .OrderBy(p => p.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<bool> UpdateStatusWithHistoryAsync(
            string procurementId,
            ProcurementStatus newStatus,
            string? changedByUserId = null,
            string? note = null,
            CancellationToken ct = default)
        {
            var procurement = await _context.Procurements
                .FirstOrDefaultAsync(p => p.ProcurementId == procurementId, ct);

            if (procurement == null)
                return false;

            // Update status
            var oldStatus = procurement.ProcurementStatus;
            procurement.ProcurementStatus = newStatus;
            procurement.UpdatedAt = DateTime.UtcNow;

            // Create history entry
            var historyEntry = new ProcurementStatusHistory
            {
                Id = Guid.NewGuid().ToString(),
                ProcurementId = procurementId,
                Status = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = changedByUserId,
                Note = note ?? $"Status changed from {oldStatus} to {newStatus}"
            };

            _context.ProcurementStatusHistories.Add(historyEntry);

            try
            {
                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IReadOnlyList<ProcurementStatusCountDto>> GetCountByProcurementStatusAsync(CancellationToken ct = default)
        {
            return await _context.Procurements
                .GroupBy(p => p.ProcurementStatus)
                .Select(group => new ProcurementStatusCountDto
                {
                    Status = group.Key.ToString(),
                    Count = group.Count()
                })
                .ToListAsync(ct);
        }

        #endregion
    }
}
