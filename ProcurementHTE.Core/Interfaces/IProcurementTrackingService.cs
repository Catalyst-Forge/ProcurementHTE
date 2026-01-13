using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProcurementTrackingService
    {
        /// <summary>
        /// Get tracking info untuk Procurement berdasarkan Procurement ID
        /// </summary>
        Task<ProcurementTrackingDto?> GetTrackingByProcurementIdAsync(string procurementId, CancellationToken ct = default);

        /// <summary>
        /// Get tracking info untuk Procurement berdasarkan ProcNum (WO number)
        /// </summary>
        Task<ProcurementTrackingDto?> GetTrackingByProcNumAsync(string procNum, CancellationToken ct = default);

        /// <summary>
        /// Get PR with all linked procurements tracking info
        /// </summary>
        Task<PRWithProcurementsTrackingDto?> GetPrWithProcurementsTrackingAsync(string prId, CancellationToken ct = default);

        /// <summary>
        /// Update status Procurement dan log ke history
        /// </summary>
        Task<bool> UpdateProcurementStatusAsync(
            string procurementId,
            ProcurementStatus newStatus,
            string? changedByUserId = null,
            string? note = null,
            CancellationToken ct = default
        );

        /// <summary>
        /// Submit No ISPA untuk procurement (status: OnSubmitISPA → OnSubmitHardcopy)
        /// </summary>
        Task<ProcurementTrackingResponse> SubmitIspaAsync(
            string procurementId,
            string ispaNumber,
            string submittedByUserId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Submit Justification document with hardcopy evidence untuk procurement (status: OnSubmitHardcopy → OnSubmitPO)
        /// </summary>
        Task<ProcurementTrackingResponse> SubmitJustificationAsync(
            string procurementId,
            string submittedByUserId,
            string hardcopyEvidenceFileName,
            string hardcopyEvidenceContentType,
            long hardcopyEvidenceFileSize,
            Stream fileStream,
            CancellationToken ct = default
        );

        /// <summary>
        /// Submit No PO dari SCM untuk procurement (status: OnSubmitPO → DonePO)
        /// </summary>
        Task<ProcurementTrackingResponse> SubmitPoAsync(
            string procurementId,
            string poNumber,
            string submittedByUserId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Send Procurement for approval - generates QR token and updates status
        /// </summary>
        Task<ProcurementTrackingResponse> SendForApprovalAsync(
            string procurementId,
            string sentByUserId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Reject Procurement dengan note (status → Rejected)
        /// </summary>
        Task<ProcurementTrackingResponse> RejectProcurementAsync(
            string procurementId,
            string rejectionNote,
            string rejectedByUserId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Trigger status change dari approval (digunakan oleh ApprovalService)
        /// Workflow: WaitingApprovalAnalyst → WaitingApprovalAsstManager → WaitingApprovalManager → OnSubmitISPA
        /// </summary>
        Task<bool> HandleApprovalStatusChangeAsync(
            string procurementId,
            string approvalAction,
            string? approverUserId = null,
            string? note = null,
            CancellationToken ct = default
        );

        /// <summary>
        /// Recalculate dan update PR status berdasarkan linked procurements
        /// Called after any procurement status change
        /// </summary>
        Task RecalculatePrStatusAsync(string prId, CancellationToken ct = default);

        /// <summary>
        /// Get document count for a procurement (uploaded/total)
        /// </summary>
        Task<(int uploaded, int total)> GetDocumentCountAsync(string procurementId, CancellationToken ct = default);

        /// <summary>
        /// Get presigned URL for hardcopy evidence from MinIO
        /// </summary>
        Task<string?> GetHardcopyEvidenceUrlAsync(string procurementId, CancellationToken ct = default);
    }
}
