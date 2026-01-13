using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public class ProcurementTrackingService : IProcurementTrackingService
    {
        private readonly IProcurementRepository _procurementRepo;
        private readonly IPurchaseRequisitionTrackingRepository _prRepo;
        private readonly ILogger<ProcurementTrackingService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IQrCodeGenerator _qrCodeGenerator;
        private readonly IObjectStorage _objectStorage;
        private readonly ObjectStorageOptions _storageOptions;

        public ProcurementTrackingService(
            IProcurementRepository procurementRepo,
            IPurchaseRequisitionTrackingRepository prRepo,
            ILogger<ProcurementTrackingService> logger,
            INotificationService notificationService,
            IQrCodeGenerator qrCodeGenerator,
            IObjectStorage objectStorage,
            IOptions<ObjectStorageOptions> storageOptions
        )
        {
            _procurementRepo = procurementRepo;
            _prRepo = prRepo;
            _logger = logger;
            _notificationService = notificationService;
            _qrCodeGenerator = qrCodeGenerator;
            _objectStorage = objectStorage;
            _storageOptions = storageOptions.Value;
        }

        public async Task<ProcurementTrackingDto?> GetTrackingByProcurementIdAsync(
            string procurementId,
            CancellationToken ct = default
        )
        {
            var procurement = await _procurementRepo.GetWithTrackingDataAsync(procurementId, ct);
            if (procurement == null)
                return null;

            var needsJustifikasi = await _prRepo.GetNeedsJustifikasiMapAsync(
                new List<string> { procurementId },
                ct
            );

            return MapToDto(procurement, needsJustifikasi);
        }

        public async Task<ProcurementTrackingDto?> GetTrackingByProcNumAsync(
            string procNum,
            CancellationToken ct = default
        )
        {
            var procurement = await _procurementRepo.GetByProcNumWithTrackingAsync(procNum, ct);
            if (procurement == null)
                return null;

            var needsJustifikasi = await _prRepo.GetNeedsJustifikasiMapAsync(
                new List<string> { procurement.ProcurementId },
                ct
            );

            return MapToDto(procurement, needsJustifikasi);
        }

        public async Task<PRWithProcurementsTrackingDto?> GetPrWithProcurementsTrackingAsync(
            string prId,
            CancellationToken ct = default
        )
        {
            var pr = await _prRepo.GetWithTrackingIncludesByPrIdAsync(prId, ct);
            if (pr == null)
                return null;

            var procurements = await _procurementRepo.GetByPrIdWithTrackingAsync(prId, ct);
            var procIds = procurements.Select(p => p.ProcurementId).ToList();
            var needsJustifikasiMap = await _prRepo.GetNeedsJustifikasiMapAsync(procIds, ct);

            return new PRWithProcurementsTrackingDto
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                RequestDate = pr.RequestDate,
                Description = pr.Description,
                CurrentStatus = pr.DerivedStatus, // Use derived status!
                CurrentStatusDescription = GetStatusDescription(pr.DerivedStatus),
                Procurements = procurements.Select(p => MapToDto(p, needsJustifikasiMap)).ToList()
            };
        }

        public async Task<bool> UpdateProcurementStatusAsync(
            string procurementId,
            ProcurementStatus newStatus,
            string? changedByUserId = null,
            string? note = null,
            CancellationToken ct = default
        )
        {
            var success = await _procurementRepo.UpdateStatusWithHistoryAsync(
                procurementId,
                newStatus,
                changedByUserId,
                note,
                ct
            );

            if (success)
            {
                _logger.LogInformation(
                    "Procurement {ProcurementId} status changed to {NewStatus} by user {UserId}",
                    procurementId,
                    newStatus,
                    changedByUserId ?? "System"
                );

                // Recalculate PR status after procurement status change
                var procurement = await _procurementRepo.GetByIdAsync(procurementId);
                if (procurement?.PrId != null)
                {
                    await RecalculatePrStatusAsync(procurement.PrId, ct);
                }
            }

            return success;
        }

        public async Task<ProcurementTrackingResponse> SubmitIspaAsync(
            string procurementId,
            string ispaNumber,
            string submittedByUserId,
            CancellationToken ct = default
        )
        {
            var procurement = await _procurementRepo.GetByIdAsync(procurementId);
            if (procurement == null)
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = "Procurement tidak ditemukan."
                };

            if (procurement.ProcurementStatus != ProcurementStatus.OnSubmitISPA)
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = $"Status procurement saat ini adalah {GetProcurementStatusDescription(procurement.ProcurementStatus)}. ISPA hanya bisa disubmit saat status 'On Submit ISPA'."
                };

            procurement.IspaNumber = ispaNumber;
            procurement.IspaSubmittedAt = DateTime.UtcNow;
            procurement.IspaSubmittedByUserId = submittedByUserId;
            procurement.UpdatedAt = DateTime.UtcNow;

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurementId,
                ProcurementStatus.OnSubmitHardcopy,
                submittedByUserId,
                $"ISPA Number: {ispaNumber}",
                ct
            );

            var tracking = await GetTrackingByProcurementIdAsync(procurementId, ct);

            return new ProcurementTrackingResponse
            {
                Success = true,
                Message = "ISPA berhasil disubmit.",
                Data = tracking
            };
        }

        public async Task<ProcurementTrackingResponse> SubmitJustificationAsync(
            string procurementId,
            string submittedByUserId,
            string hardcopyEvidenceFileName,
            string hardcopyEvidenceContentType,
            long hardcopyEvidenceFileSize,
            Stream fileStream,
            CancellationToken ct = default
        )
        {
            var procurement = await _procurementRepo.GetByIdAsync(procurementId);
            if (procurement == null)
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = "Procurement tidak ditemukan."
                };

            if (procurement.ProcurementStatus != ProcurementStatus.OnSubmitHardcopy)
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = $"Status procurement saat ini adalah {GetProcurementStatusDescription(procurement.ProcurementStatus)}. Hardcopy hanya bisa disubmit saat status 'On Submit Hardcopy'."
                };

            // Upload file to MinIO with same bucket as documents
            var objectKey = $"procurements/{procurementId}/evidence-hardcopy/{Guid.NewGuid():N}-{hardcopyEvidenceFileName}";
            await _objectStorage.UploadAsync(
                _storageOptions.Bucket,
                objectKey,
                fileStream,
                hardcopyEvidenceFileSize,
                hardcopyEvidenceContentType,
                ct
            );

            procurement.HardcopyEvidenceFileName = hardcopyEvidenceFileName;
            procurement.HardcopyEvidenceFilePath = objectKey; // Store MinIO object key instead of local path
            procurement.HardcopyEvidenceContentType = hardcopyEvidenceContentType;
            procurement.HardcopyEvidenceFileSize = hardcopyEvidenceFileSize;
            procurement.HardcopySubmittedAt = DateTime.UtcNow;
            procurement.HardcopySubmittedByUserId = submittedByUserId;
            procurement.UpdatedAt = DateTime.UtcNow;

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurementId,
                ProcurementStatus.OnSubmitPO,
                submittedByUserId,
                "Hardcopy evidence uploaded",
                ct
            );

            var tracking = await GetTrackingByProcurementIdAsync(procurementId, ct);

            return new ProcurementTrackingResponse
            {
                Success = true,
                Message = "Hardcopy evidence berhasil disubmit.",
                Data = tracking
            };
        }

        public async Task<ProcurementTrackingResponse> SubmitPoAsync(
            string procurementId,
            string poNumber,
            string submittedByUserId,
            CancellationToken ct = default
        )
        {
            var procurement = await _procurementRepo.GetByIdAsync(procurementId);
            if (procurement == null)
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = "Procurement tidak ditemukan."
                };

            if (procurement.ProcurementStatus != ProcurementStatus.OnSubmitPO)
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = $"Status procurement saat ini adalah {GetProcurementStatusDescription(procurement.ProcurementStatus)}. PO hanya bisa disubmit saat status 'On Submit PO'."
                };

            procurement.PoNumber = poNumber;
            procurement.PoSubmittedAt = DateTime.UtcNow;
            procurement.PoSubmittedByUserId = submittedByUserId;
            procurement.UpdatedAt = DateTime.UtcNow;

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurementId,
                ProcurementStatus.DonePO,
                submittedByUserId,
                $"PO Number: {poNumber}",
                ct
            );

            var tracking = await GetTrackingByProcurementIdAsync(procurementId, ct);

            return new ProcurementTrackingResponse
            {
                Success = true,
                Message = "PO berhasil disubmit. Procurement selesai!",
                Data = tracking
            };
        }

        public async Task<ProcurementTrackingResponse> SendForApprovalAsync(
            string procurementId,
            string sentByUserId,
            CancellationToken ct = default
        )
        {
            var procurement = await _procurementRepo.GetByIdAsync(procurementId);
            if (procurement == null)
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = "Procurement tidak ditemukan."
                };

            if (procurement.ProcurementStatus != ProcurementStatus.OnCreateDP3)
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = $"Status procurement saat ini adalah {GetProcurementStatusDescription(procurement.ProcurementStatus)}. Approval hanya bisa dikirim saat status 'On Create DP3'."
                };

            // Check mandatory documents
            var procIds = new List<string> { procurementId };
            var needsJustifikasiMap = await _prRepo.GetNeedsJustifikasiMapAsync(procIds, ct);
            var (totalDocs, uploadedDocs) = CountMandatoryDocs(procurement, needsJustifikasiMap);

            if (totalDocs > 0 && uploadedDocs < totalDocs)
            {
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = $"Dokumen wajib belum lengkap. {uploadedDocs}/{totalDocs} dokumen telah diupload."
                };
            }

            // Generate approval token
            var approvalToken = GenerateApprovalToken();
            procurement.ApprovalToken = approvalToken;
            procurement.ApprovalTokenGeneratedAt = DateTime.UtcNow;
            procurement.ApprovalSentByUserId = sentByUserId;
            procurement.UpdatedAt = DateTime.UtcNow;

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurementId,
                ProcurementStatus.WaitingApprovalAnalyst,
                sentByUserId,
                "Sent for Analyst HTE approval",
                ct
            );

            // Send notification to Analyst HTE
            await SendApprovalNotification(procurement, ct);

            var tracking = await GetTrackingByProcurementIdAsync(procurementId, ct);

            return new ProcurementTrackingResponse
            {
                Success = true,
                Message = "Procurement berhasil dikirim untuk approval.",
                Data = tracking
            };
        }

        public async Task<ProcurementTrackingResponse> RejectProcurementAsync(
            string procurementId,
            string rejectionNote,
            string rejectedByUserId,
            CancellationToken ct = default
        )
        {
            var procurement = await _procurementRepo.GetByIdAsync(procurementId);
            if (procurement == null)
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = "Procurement tidak ditemukan."
                };

            procurement.RejectionNote = rejectionNote;
            procurement.RejectedAt = DateTime.UtcNow;
            procurement.RejectedByUserId = rejectedByUserId;
            procurement.UpdatedAt = DateTime.UtcNow;

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurementId,
                ProcurementStatus.Rejected,
                rejectedByUserId,
                $"Rejected: {rejectionNote}",
                ct
            );

            var tracking = await GetTrackingByProcurementIdAsync(procurementId, ct);

            return new ProcurementTrackingResponse
            {
                Success = true,
                Message = "Procurement telah ditolak.",
                Data = tracking
            };
        }

        public async Task<bool> HandleApprovalStatusChangeAsync(
            string procurementId,
            string approvalAction,
            string? approverUserId = null,
            string? note = null,
            CancellationToken ct = default
        )
        {
            var procurement = await _procurementRepo.GetByIdAsync(procurementId);
            if (procurement == null)
                return false;

            var currentStatus = procurement.ProcurementStatus;

            if (approvalAction.Equals("approve", StringComparison.OrdinalIgnoreCase))
            {
                var newStatus = currentStatus switch
                {
                    ProcurementStatus.WaitingApprovalAnalyst => ProcurementStatus.WaitingApprovalAsstManager,
                    ProcurementStatus.WaitingApprovalAsstManager => ProcurementStatus.WaitingApprovalManager,
                    ProcurementStatus.WaitingApprovalManager => ProcurementStatus.OnSubmitISPA,
                    _ => currentStatus
                };

                if (newStatus != currentStatus)
                {
                    await UpdateProcurementStatusAsync(
                        procurementId,
                        newStatus,
                        approverUserId,
                        note ?? "Approved",
                        ct
                    );

                    // Send notification to next approver or APPO
                    if (newStatus == ProcurementStatus.OnSubmitISPA)
                    {
                        // Notify APPO that procurement ready for ISPA submission
                        await SendReadyForIspaNotification(procurement, ct);
                    }
                    else
                    {
                        // Send to next approver
                        await SendApprovalNotification(procurement, ct);
                    }

                    return true;
                }
            }
            else if (approvalAction.Equals("reject", StringComparison.OrdinalIgnoreCase))
            {
                procurement.RejectionNote = note ?? "Rejected by approver";
                procurement.RejectedAt = DateTime.UtcNow;
                procurement.RejectedByUserId = approverUserId;
                procurement.UpdatedAt = DateTime.UtcNow;

                await _procurementRepo.UpdateProcurementAsync(procurement);
                await UpdateProcurementStatusAsync(
                    procurementId,
                    ProcurementStatus.Rejected,
                    approverUserId,
                    note ?? "Rejected",
                    ct
                );

                return true;
            }

            return false;
        }

        public async Task RecalculatePrStatusAsync(string prId, CancellationToken ct = default)
        {
            var pr = await _prRepo.GetWithTrackingIncludesByPrIdAsync(prId, ct);
            if (pr == null)
                return;

            // Use DerivedStatus from the property
            var newDerivedStatus = pr.DerivedStatus;

            // Update PR status in database if it changed
            if (pr.Status != newDerivedStatus)
            {
                var oldStatus = pr.Status;
                pr.Status = newDerivedStatus;
                pr.UpdatedAt = DateTime.UtcNow;

                await _prRepo.UpdateAsync(pr, ct);
                await _prRepo.AddStatusHistoryAsync(prId, newDerivedStatus, null, "Recalculated from procurement statuses", ct);
                await _prRepo.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "PR {PrId} status recalculated from {OldStatus} to {NewStatus}",
                    prId,
                    oldStatus,
                    newDerivedStatus
                );
            }
        }

        #region Helper Methods

        private ProcurementTrackingDto MapToDto(
            Procurement procurement,
            Dictionary<string, bool> needsJustifikasiMap
        )
        {
            var needsJustifikasi = needsJustifikasiMap.GetValueOrDefault(procurement.ProcurementId, false);
            var (totalDocs, uploadedDocs) = CountMandatoryDocs(procurement, needsJustifikasiMap);

            return new ProcurementTrackingDto
            {
                ProcurementId = procurement.ProcurementId,
                ProcNum = procurement.ProcNum ?? string.Empty,
                Wonum = procurement.Wonum,
                JobName = procurement.JobName ?? string.Empty,
                DocumentDate = procurement.DocumentDate,
                CurrentStatus = procurement.ProcurementStatus,
                CurrentStatusDescription = GetProcurementStatusDescription(procurement.ProcurementStatus),
                PrId = procurement.PrId,
                PrNumber = procurement.PurchaseRequisition?.PrNumber,
                IspaNumber = procurement.IspaNumber,
                IspaSubmittedAt = procurement.IspaSubmittedAt,
                IspaSubmittedByUserName = procurement.IspaSubmittedByUser?.FullName,
                PoNumber = procurement.PoNumber,
                PoSubmittedAt = procurement.PoSubmittedAt,
                PoSubmittedByUserName = procurement.PoSubmittedByUser?.FullName,
                HardcopyEvidenceFileName = procurement.HardcopyEvidenceFileName,
                HardcopyEvidenceFilePath = procurement.HardcopyEvidenceFilePath,
                HardcopySubmittedAt = procurement.HardcopySubmittedAt,
                HardcopySubmittedByUserName = procurement.HardcopySubmittedByUser?.FullName,
                RejectionNote = procurement.RejectionNote,
                RejectedAt = procurement.RejectedAt,
                RejectedByUserName = procurement.RejectedByUser?.FullName,
                ApprovalToken = procurement.ApprovalToken,
                ApprovalTokenGeneratedAt = procurement.ApprovalTokenGeneratedAt,
                ApprovalSentByUserName = procurement.ApprovalSentByUser?.FullName,
                AppoUserId = procurement.AppoUserId,
                StatusHistory = procurement.StatusHistories?.Select(MapToHistoryDto).ToList() ?? new List<ProcurementStatusHistoryDto>(),
                TotalMandatoryDocs = totalDocs,
                UploadedMandatoryDocs = uploadedDocs,
                TotalDocuments = procurement.ProcDocuments?.Count ?? 0,
                ApprovalQrUrl = !string.IsNullOrEmpty(procurement.ApprovalToken)
                    ? GenerateQrCodeDataUri(procurement.ApprovalToken)
                    : null,
                NextApproverRole = GetNextApproverRole(procurement.ProcurementStatus),
                NextApproverName = GetNextApproverName(procurement)
            };
        }

        private static ProcurementStatusHistoryDto MapToHistoryDto(ProcurementStatusHistory history)
        {
            return new ProcurementStatusHistoryDto
            {
                Id = history.Id,
                Status = history.Status,
                StatusDescription = GetProcurementStatusDescription(history.Status),
                ChangedAt = history.ChangedAt,
                ChangedByUserName = history.ChangedByUser?.UserName,
                ChangedByFullName = history.ChangedByUser?.FullName,
                Note = history.Note
            };
        }

        private (int totalDocs, int uploadedDocs) CountMandatoryDocs(
            Procurement procurement,
            Dictionary<string, bool> needsJustifikasiMap
        )
        {
            var needsJustifikasi = needsJustifikasiMap.GetValueOrDefault(procurement.ProcurementId, false);

            // Get ALL required documents from job type (not just mandatory ones)
            // This matches the logic in ProcurementDocumentQuery.GetRequiredDocsAsync
            // Filter by ProcurementCategory (null = applies to all categories)
            // Exclude "Justifikasi" if not needed
            var requiredDocTypes = procurement.JobType?.JobTypeDocuments?
                .Where(jtd => 
                    (jtd.ProcurementCategory == null || jtd.ProcurementCategory == procurement.ProcurementCategory))
                .ToList() ?? new List<Models.JobTypeDocuments>();

            // Filter out Justifikasi if not needed (bestFinalOffer <= 300juta)
            if (!needsJustifikasi)
            {
                requiredDocTypes = requiredDocTypes
                    .Where(jtd => jtd.DocumentType?.Name != "Justifikasi")
                    .ToList();
            }

            var requiredDocTypeIds = requiredDocTypes
                .Select(jtd => jtd.DocumentTypeId)
                .Distinct()
                .ToHashSet();

            var totalDocs = requiredDocTypeIds.Count;

            // Count uploaded documents (match by DocumentTypeId)
            var uploadedDocTypeIds = procurement.ProcDocuments?
                .Where(pd => !string.IsNullOrEmpty(pd.DocumentTypeId))
                .Select(pd => pd.DocumentTypeId!)
                .ToHashSet() ?? new HashSet<string>();

            var uploadedDocs = requiredDocTypeIds.Count(dtId => uploadedDocTypeIds.Contains(dtId));

            return (totalDocs, uploadedDocs);
        }

        private static string GenerateApprovalToken()
        {
            var shortGuid = Guid.NewGuid().ToString("N")[..8];
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return $"APPR-{shortGuid}-{timestamp}";
        }

        /// <summary>
        /// Generates QR code as base64 data URI using local library (no external API).
        /// </summary>
        private string GenerateQrCodeDataUri(string approvalToken)
        {
            var deepLink = $"procurehte://approve/{approvalToken}";
            return _qrCodeGenerator.GenerateAsDataUri(deepLink, 10);
        }

        private static string? GetNextApproverRole(ProcurementStatus status)
        {
            return status switch
            {
                ProcurementStatus.WaitingApprovalAnalyst => "Analyst HTE",
                ProcurementStatus.WaitingApprovalAsstManager => "Assistant Manager",
                ProcurementStatus.WaitingApprovalManager => "Manager",
                _ => null
            };
        }

        private string? GetNextApproverName(Procurement procurement)
        {
            return procurement.ProcurementStatus switch
            {
                ProcurementStatus.WaitingApprovalAnalyst => procurement.AnalystHteUserId,
                ProcurementStatus.WaitingApprovalAsstManager => procurement.AssistantManagerUserId,
                ProcurementStatus.WaitingApprovalManager => procurement.ManagerUserId,
                _ => null
            };
        }

        private static string GetStatusDescription(PurchaseRequisitionStatus status)
        {
            var field = status.GetType().GetField(status.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? status.ToString();
        }

        private static string GetProcurementStatusDescription(ProcurementStatus status)
        {
            var field = status.GetType().GetField(status.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? status.ToString();
        }

        private async Task SendApprovalNotification(Procurement procurement, CancellationToken ct)
        {
            var approverUserId = procurement.ProcurementStatus switch
            {
                ProcurementStatus.WaitingApprovalAnalyst => procurement.AnalystHteUserId,
                ProcurementStatus.WaitingApprovalAsstManager => procurement.AssistantManagerUserId,
                ProcurementStatus.WaitingApprovalManager => procurement.ManagerUserId,
                _ => null
            };

            if (string.IsNullOrEmpty(approverUserId))
                return;

            await _notificationService.SendNotificationAsync(
                userId: approverUserId,
                title: $"Procurement {procurement.ProcNum} menunggu approval Anda",
                message: $"WO: {procurement.Wonum} - {procurement.JobName}",
                notificationType: "ApprovalRequest",
                actionUrl: $"/Procurements/Details/{procurement.ProcurementId}",
                referenceId: procurement.ProcurementId,
                createdByUserId: procurement.ApprovalSentByUserId,
                ct: ct
            );
        }

        private async Task SendReadyForIspaNotification(Procurement procurement, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(procurement.AppoUserId))
                return;

            await _notificationService.SendNotificationAsync(
                userId: procurement.AppoUserId,
                title: $"Procurement {procurement.ProcNum} siap untuk submit ISPA",
                message: $"WO: {procurement.Wonum} - Semua approval telah selesai",
                notificationType: "ReadyForISPA",
                actionUrl: $"/Procurements/Details/{procurement.ProcurementId}",
                referenceId: procurement.ProcurementId,
                createdByUserId: null,
                ct: ct
            );
        }

        #endregion

        #region Document Count

        /// <inheritdoc/>
        public async Task<(int uploaded, int total)> GetDocumentCountAsync(string procurementId, CancellationToken ct = default)
        {
            var procurement = await _procurementRepo.GetWithTrackingDataAsync(procurementId, ct);
            if (procurement == null)
                return (0, 0);

            var needsJustifikasiMap = await _prRepo.GetNeedsJustifikasiMapAsync(
                new List<string> { procurementId },
                ct
            );

            var (totalDocs, uploadedDocs) = CountMandatoryDocs(procurement, needsJustifikasiMap);
            return (uploadedDocs, totalDocs);
        }

        #endregion

        #region Hardcopy Evidence

        /// <inheritdoc/>
        public async Task<string?> GetHardcopyEvidenceUrlAsync(string procurementId, CancellationToken ct = default)
        {
            var procurement = await _procurementRepo.GetByIdAsync(procurementId);
            if (procurement == null || string.IsNullOrEmpty(procurement.HardcopyEvidenceFilePath))
                return null;

            // Get presigned URL from MinIO (valid for 1 hour)
            return await _objectStorage.GetPresignedUrlAsync(
                _storageOptions.Bucket,
                procurement.HardcopyEvidenceFilePath,
                TimeSpan.FromHours(1),
                ct
            );
        }

        #endregion
    }
}
