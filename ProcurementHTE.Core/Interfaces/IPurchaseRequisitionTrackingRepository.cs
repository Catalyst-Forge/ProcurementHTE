using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    /// <summary>
    /// Repository interface for Purchase Requisition Tracking data access operations.
    /// Handles all database queries related to PR tracking workflow.
    /// </summary>
    public interface IPurchaseRequisitionTrackingRepository
    {
        /// <summary>
        /// Get PR with full tracking includes by PR Number
        /// </summary>
        Task<PurchaseRequisition?> GetWithTrackingIncludesByPrNumberAsync(
            string prNumber,
            CancellationToken ct = default
        );

        /// <summary>
        /// Get PR with full tracking includes by PR ID
        /// </summary>
        Task<PurchaseRequisition?> GetWithTrackingIncludesByPrIdAsync(
            string prId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Get PR by ID (simple, no includes)
        /// </summary>
        Task<PurchaseRequisition?> GetByIdAsync(string prId, CancellationToken ct = default);

        /// <summary>
        /// Get PR by ID with Procurements included
        /// </summary>
        Task<PurchaseRequisition?> GetByIdWithProcurementsAsync(
            string prId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Get User by ID
        /// </summary>
        Task<User?> GetUserByIdAsync(string userId, CancellationToken ct = default);

        /// <summary>
        /// Update PR entity
        /// </summary>
        Task UpdateAsync(PurchaseRequisition pr, CancellationToken ct = default);

        /// <summary>
        /// Add status history record
        /// </summary>
        Task AddStatusHistoryAsync(
            string prId,
            PurchaseRequisitionStatus status,
            string? changedByUserId,
            string? note,
            CancellationToken ct = default
        );

        /// <summary>
        /// Get map of procurement ID to whether it needs Justifikasi document (value > 300 million)
        /// </summary>
        Task<Dictionary<string, bool>> GetNeedsJustifikasiMapAsync(
            List<string> procurementIds,
            CancellationToken ct = default
        );

        /// <summary>
        /// Save all pending changes
        /// </summary>
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
