using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProcurementService
    {
        // Query Methods
        Task<PagedResult<Procurement>> GetAllProcurementWithDetailsAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct,
            string? userId = null
        );
        Task<Procurement?> GetProcurementByIdAsync(string id);
        Task<IReadOnlyList<Procurement>> GetMyRecentProcurementAsync(
            string userId,
            int limit,
            CancellationToken ct
        );
        Task<int> CountAllProcurementsAsync(CancellationToken ct);
        Task<PagedResult<Procurement>> GetProcurementsForAppoApprovalAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );

        /// <summary>
        /// Get procurements that have been picked up by a specific AP-PO user
        /// </summary>
        Task<PagedResult<Procurement>> GetMyAppoPickupsAsync(
            string appoUserId,
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );

        // Lookup Methods
        Task<(
            List<JobTypes> JobTypes,
            List<Status> Statuses
        )> GetRelatedEntitiesForProcurementAsync();
        Task<JobTypes?> GetJobTypeByIdAsync(string id);
        Task<Status?> GetStatusByNameAsync(string name);

        // Command Methods
        Task AddProcurementWithDetailsAsync(
            Procurement procurement,
            List<ProcDetail> details,
            List<ProcOffer> offers
        );
        Task EditProcurementAsync(
            Procurement procurement,
            string id,
            List<ProcDetail> details,
            List<ProcOffer> offers
        );
        Task DeleteProcurementAsync(Procurement procurement, string deletedByUserId);
        Task MarkAsCompletedAsync(string procurementId);
        Task ApproveByAppoAsync(string procurementId, string appoUserId);
        Task RejectByAppoAsync(string procurementId);
        Task PublishAsync(string procurementId);
        Task UnpublishAsync(string procurementId);
        Task PickupAsync(string procurementId, string appoUserId);
        
        /// <summary>
        /// Update accrual data for a procurement (filled by AR role)
        /// </summary>
        Task UpdateAccrualDataAsync(
            string procurementId, 
            string? noAccrual, 
            decimal? potensiAccrual, 
            string? statusAccrual,
            string filledByUserId
        );

        /// <summary>
        /// Get procurements that need accrual data to be filled
        /// </summary>
        Task<PagedResult<Procurement>> GetProcurementsForAccrualAsync(
            int page,
            int pageSize,
            string? search,
            string? filter, // "pending", "filled", "all"
            CancellationToken ct
        );

        /// <summary>
        /// Pickup procurement for AP-Invoice processing
        /// </summary>
        Task PickupForApInvoiceAsync(string procurementId, string apInvoiceUserId);

        /// <summary>
        /// Update invoice data (SA No, SP 3 No)
        /// </summary>
        Task UpdateInvoiceDataAsync(
            string procurementId,
            string? saNo,
            string? sp3No,
            string filledByUserId
        );

        /// <summary>
        /// Pickup procurement for AR accrual processing
        /// </summary>
        Task PickupForArAsync(string procurementId, string arUserId);

        /// <summary>
        /// Get procurements for AP-Invoice pickup (picked by AP-PO)
        /// </summary>
        Task<PagedResult<Procurement>> GetProcurementsForApInvoiceAsync(
            int page,
            int pageSize,
            string? search,
            string? filter, // "pending", "filled", "all"
            CancellationToken ct
        );

        /// <summary>
        /// Get procurements that have been picked up by a specific AP-Invoice user
        /// </summary>
        Task<PagedResult<Procurement>> GetMyApInvoicePickupsAsync(
            string apInvoiceUserId,
            int page,
            int pageSize,
            string? search,
            CancellationToken ct
        );

        /// <summary>
        /// Get procurements for AR pickup (picked by AP-Invoice)
        /// </summary>
        Task<PagedResult<Procurement>> GetProcurementsForArPickupAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        );

        /// <summary>
        /// Get procurements that have been picked up by a specific AR user
        /// </summary>
        Task<PagedResult<Procurement>> GetMyArPickupsAsync(
            string arUserId,
            int page,
            int pageSize,
            string? search,
            CancellationToken ct
        );
    }
}
