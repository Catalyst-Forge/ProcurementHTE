using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProcurementRepository
    {
        // Query Methods
        Task<Common.PagedResult<Procurement>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct,
            string? userId = null
        );
        Task<Procurement?> GetByIdAsync(string id);
        Task<Procurement?> GetWithSelectedOfferAsync(string id);
        Task<IReadOnlyList<Procurement>> GetRecentByUserAsync(
            string userId,
            int limit,
            CancellationToken ct
        );
        Task<int> CountAsync(CancellationToken ct);
        Task<IReadOnlyList<ProcurementStatusCountDto>> GetCountByStatusAsync();

        /// <summary>
        /// Get all procurements for selection in PR with related data (JobType, Status, ProfitLosses with SelectedVendor)
        /// </summary>
        Task<IReadOnlyList<Procurement>> GetAllForSelectionAsync();

        /// <summary>
        /// Get all procurements with status "Waiting Pickup" for AP-PO pickup
        /// </summary>
        Task<Common.PagedResult<Procurement>> GetProcurementsForAppoApprovalAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );

        /// <summary>
        /// Get procurements that have been picked up by a specific AP-PO user
        /// </summary>
        Task<Common.PagedResult<Procurement>> GetMyAppoPickupsAsync(
            string appoUserId,
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );

        // Lookup Methods
        Task<Status?> GetStatusByNameAsync(string name);
        Task<List<Status>> GetStatusesAsync();
        Task<JobTypes?> GetJobTypeByIdAsync(string id);
        Task<List<JobTypes>> GetJobTypesAsync();

        // Command Methods
        Task StoreProcurementWithDetailsAsync(
            Procurement procurement,
            List<ProcDetail> details,
            List<ProcOffer> offers
        );
        Task UpdateProcurementAsync(Procurement procurement);
        Task UpdateProcurementWithDetailsAsync(
            Procurement procurement,
            List<ProcDetail> details,
            List<ProcOffer> offers
        );
        Task DeleteAsync(Procurement procurement, string deletedByUserId);

        /// <summary>
        /// Get procurements that need accrual data (for AR role)
        /// </summary>
        Task<Common.PagedResult<Procurement>> GetProcurementsForAccrualAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        );

        /// <summary>
        /// Get procurements for AP-Invoice pickup (picked by AP-PO but not yet by AP-Invoice)
        /// </summary>
        Task<Common.PagedResult<Procurement>> GetProcurementsForApInvoiceAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        );

        /// <summary>
        /// Get procurements that have been picked up by a specific AP-Invoice user
        /// </summary>
        Task<Common.PagedResult<Procurement>> GetMyApInvoicePickupsAsync(
            string apInvoiceUserId,
            int page,
            int pageSize,
            string? search,
            CancellationToken ct
        );

        /// <summary>
        /// Get procurements for AR pickup (picked by AP-Invoice but not yet by AR)
        /// </summary>
        Task<Common.PagedResult<Procurement>> GetProcurementsForArPickupAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        );

        /// <summary>
        /// Get procurements that have been picked up by a specific AR user
        /// </summary>
        Task<Common.PagedResult<Procurement>> GetMyArPickupsAsync(
            string arUserId,
            int page,
            int pageSize,
            string? search,
            CancellationToken ct
        );

        // ===== Procurement Tracking Methods =====

        /// <summary>
        /// Get procurement with tracking data (includes StatusHistories, User navigations)
        /// </summary>
        Task<Procurement?> GetWithTrackingDataAsync(string procurementId, CancellationToken ct = default);

        /// <summary>
        /// Get procurement by ProcNum or Wonum with tracking data
        /// </summary>
        Task<Procurement?> GetByProcNumWithTrackingAsync(string procNum, CancellationToken ct = default);

        /// <summary>
        /// Get all procurements linked to a PR with tracking data
        /// </summary>
        Task<IReadOnlyList<Procurement>> GetByPrIdWithTrackingAsync(string prId, CancellationToken ct = default);

        /// <summary>
        /// Update procurement status and create status history entry
        /// </summary>
        Task<bool> UpdateStatusWithHistoryAsync(
            string procurementId,
            ProcurementStatus newStatus,
            string? changedByUserId = null,
            string? note = null,
            CancellationToken ct = default
        );

        /// <summary>
        /// Get procurement count by status for dashboard
        /// </summary>
        Task<IReadOnlyList<ProcurementStatusCountDto>> GetCountByProcurementStatusAsync(CancellationToken ct = default);
    }
}
