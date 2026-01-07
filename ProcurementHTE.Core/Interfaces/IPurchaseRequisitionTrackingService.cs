using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IPurchaseRequisitionTrackingService
    {
        /// <summary>
        /// Get tracking info untuk PR berdasarkan PR Number
        /// </summary>
        Task<PRTrackingDto?> GetTrackingByPrNumberAsync(string prNumber, CancellationToken ct = default);

        /// <summary>
        /// Get tracking info untuk PR berdasarkan PR ID
        /// </summary>
        Task<PRTrackingDto?> GetTrackingByPrIdAsync(string prId, CancellationToken ct = default);

        /// <summary>
        /// Update status PR dan log ke history
        /// </summary>
        Task<bool> UpdatePrStatusAsync(
            string prId,
            PurchaseRequisitionStatus newStatus,
            string? changedByUserId = null,
            string? note = null,
            CancellationToken ct = default
        );

        /// <summary>
        /// Submit No ISPA (status: OnSubmitISPA → OnSubmitHardcopy)
        /// </summary>
        Task<PRTrackingResponse> SubmitIspaAsync(
            string prId,
            string ispaNumber,
            string submittedByUserId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Submit Justification document (status: OnSubmitHardcopy → OnSubmitPO)
        /// </summary>
        Task<PRTrackingResponse> SubmitJustificationAsync(
            string prId,
            string submittedByUserId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Submit No PO dari SCM (status: OnSubmitPO → DonePO)
        /// </summary>
        Task<PRTrackingResponse> SubmitPoAsync(
            string prId,
            string poNumber,
            string submittedByUserId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Send PR for approval - generates QR token and updates status
        /// </summary>
        Task<PRTrackingResponse> SendForApprovalAsync(
            string prId,
            string sentByUserId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Reject PR dengan note (status → Rejected)
        /// </summary>
        Task<PRTrackingResponse> RejectPrAsync(
            string prId,
            string rejectionNote,
            string rejectedByUserId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Trigger status change dari approval (digunakan oleh ApprovalService)
        /// </summary>
        Task<bool> HandleApprovalStatusChangeAsync(
            string prId,
            string approvalAction,
            string? approverUserId = null,
            string? note = null,
            CancellationToken ct = default
        );
    }
}
