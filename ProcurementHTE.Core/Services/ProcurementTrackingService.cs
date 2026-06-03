using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Constants;
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
        private readonly IUserRepository _userRepo;
        private readonly ILogger<ProcurementTrackingService> _logger;
        private readonly INotificationService _notificationService;
        private readonly INotificationPusher _notificationPusher;
        private readonly IQrCodeGenerator _qrCodeGenerator;
        private readonly IObjectStorage _objectStorage;
        private readonly ObjectStorageOptions _storageOptions;

        public ProcurementTrackingService(
            IProcurementRepository procurementRepo,
            IPurchaseRequisitionTrackingRepository prRepo,
            IUserRepository userRepo,
            ILogger<ProcurementTrackingService> logger,
            INotificationService notificationService,
            INotificationPusher notificationPusher,
            IQrCodeGenerator qrCodeGenerator,
            IObjectStorage objectStorage,
            IOptions<ObjectStorageOptions> storageOptions
        )
        {
            _procurementRepo = procurementRepo;
            _prRepo = prRepo;
            _userRepo = userRepo;
            _logger = logger;
            _notificationService = notificationService;
            _notificationPusher = notificationPusher;
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
            DateTime ispaDate,
            DateTime ispaSubmitDate,
            string ispaFileName,
            string ispaContentType,
            long ispaFileSize,
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

            if (procurement.ProcurementStatus != ProcurementStatus.OnSubmitISPA)
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = $"Status procurement saat ini adalah {GetProcurementStatusDescription(procurement.ProcurementStatus)}. ISPA hanya bisa disubmit saat status 'On Submit ISPA'."
                };

            // Upload file to object storage
            var objectKey = $"procurements/{procurementId}/ispa/{Guid.NewGuid():N}-{ispaFileName}";
            try
            {
                await _objectStorage.UploadAsync(
                    _storageOptions.Bucket,
                    objectKey,
                    fileStream,
                    ispaFileSize,
                    ispaContentType,
                    ct
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload ISPA file for procurement {ProcurementId}", procurementId);
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = "Gagal mengupload file ISPA. Silakan coba lagi."
                };
            }

            procurement.IspaNumber = ispaNumber;
            procurement.IspaDate = ispaDate;
            procurement.IspaSubmitDate = ispaSubmitDate;
            procurement.IspaSubmittedAt = DateTime.UtcNow;
            procurement.IspaSubmittedByUserId = submittedByUserId;
            procurement.IspaFileName = ispaFileName;
            procurement.IspaFileObjectKey = objectKey;
            procurement.IspaFileContentType = ispaContentType;
            procurement.IspaFileSize = ispaFileSize;
            procurement.UpdatedAt = DateTime.UtcNow;

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurementId,
                ProcurementStatus.OnSubmitHardcopy,
                submittedByUserId,
                $"ISPA Number: {ispaNumber}, Tanggal ISPA: {ispaDate:dd MMM yyyy}, Tanggal Submit: {ispaSubmitDate:dd MMM yyyy}",
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

            // Auto-assign VP/OpDir/PresDir based on CT value
            await AssignHigherLevelApproversAsync(procurement, ct);

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurementId,
                ProcurementStatus.WaitingApprovalAnalyst,
                sentByUserId,
                "Sent for Analyst HTE approval",
                ct
            );

            // Refresh procurement to get updated user references
            procurement = await _procurementRepo.GetByIdAsync(procurementId);
            if (procurement != null)
            {
                // Send notification to Analyst HTE
                await SendApprovalNotification(procurement, ct);
            }

            var tracking = await GetTrackingByProcurementIdAsync(procurementId, ct);

            return new ProcurementTrackingResponse
            {
                Success = true,
                Message = "Procurement berhasil dikirim untuk approval.",
                Data = tracking
            };
        }

        /// <summary>
        /// Auto-assign VP, Operation Director, and President Director based on CT value.
        /// Users are resolved from their respective roles.
        /// </summary>
        private async Task AssignHigherLevelApproversAsync(Procurement procurement, CancellationToken ct)
        {
            // Get CT (Final Offer) from PNL
            var ctValue = await GetCtAsync(procurement.ProcurementId);
            var requiredLevel = ApprovalConstants.GetRequiredApprovalLevel(ctValue);

            _logger.LogInformation(
                "Procurement {ProcNum}: CT = {CT:N0}, Required Approval Level = {Level}",
                procurement.ProcNum,
                ctValue,
                requiredLevel
            );

            // Assign VP if required (CT > 500M)
            if (requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_VP && string.IsNullOrEmpty(procurement.VicePresidentUserId))
            {
                var vpUser = await ResolveFirstUserByRoleAsync("Vice President", ct);
                if (vpUser != null)
                {
                    procurement.VicePresidentUserId = vpUser.UserId;
                    _logger.LogInformation("Assigned VP: {FullName}", vpUser.FullName);
                }
            }

            // Assign Operation Director if required (CT > 5B)
            if (requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_OP_DIR && string.IsNullOrEmpty(procurement.OperationDirectorUserId))
            {
                var opDirUser = await ResolveFirstUserByRoleAsync("Operation Director", ct);
                if (opDirUser != null)
                {
                    procurement.OperationDirectorUserId = opDirUser.UserId;
                    _logger.LogInformation("Assigned OpDir: {FullName}", opDirUser.FullName);
                }
            }

            // Assign President Director if required (CT > 10B)
            if (requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_PRES_DIR && string.IsNullOrEmpty(procurement.PresidentDirectorUserId))
            {
                var presDirUser = await ResolveFirstUserByRoleAsync("President Director", ct);
                if (presDirUser != null)
                {
                    procurement.PresidentDirectorUserId = presDirUser.UserId;
                    _logger.LogInformation("Assigned PresDir: {FullName}", presDirUser.FullName);
                }
            }
        }

        /// <summary>
        /// Resolve the first user with the specified role name
        /// </summary>
        private async Task<UserBasicInfo?> ResolveFirstUserByRoleAsync(string roleName, CancellationToken ct)
        {
            // Use the user repository to get users by role
            var users = await _userRepo.GetUsersByRoleAsync(roleName, ct);
            return users?.FirstOrDefault();
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

        public async Task<ProcurementTrackingResponse> ReturnForRevisionAsync(
            string procurementId,
            RejectionSymptom symptoms,
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

            if (symptoms == RejectionSymptom.None)
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = "Minimal satu symptom harus dipilih."
                };

            // Save current status before rejection
            procurement.StatusBeforeRejection = procurement.ProcurementStatus;
            procurement.RejectionSymptoms = symptoms;
            procurement.PendingRevisionSymptoms = symptoms;
            procurement.RejectionNote = rejectionNote;
            procurement.RejectedAt = DateTime.UtcNow;
            procurement.RejectedByUserId = rejectedByUserId;
            procurement.UpdatedAt = DateTime.UtcNow;

            // Determine first revision status based on symptoms (Data first, then PR)
            ProcurementStatus newStatus;
            string notificationTarget;

            if (symptoms.HasDataIssues())
            {
                newStatus = ProcurementStatus.NeedsRevisionData;
                notificationTarget = "PIC_OPS";
            }
            else if (symptoms.HasPRIssues())
            {
                newStatus = ProcurementStatus.NeedsRevisionPR;
                notificationTarget = "APPO";
            }
            else
            {
                // Other symptoms - default to data revision
                newStatus = ProcurementStatus.NeedsRevisionData;
                notificationTarget = "PIC_OPS";
            }

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurementId,
                newStatus,
                rejectedByUserId,
                $"Returned for revision: {rejectionNote}. Symptoms: {string.Join(", ", symptoms.GetSelectedDisplayNames())}",
                ct
            );

            // Send notification based on target
            if (notificationTarget == "PIC_OPS")
            {
                await SendRevisionNotificationToPicOps(procurement, symptoms, rejectionNote, ct);
            }
            else
            {
                await SendRevisionNotificationToAppo(procurement, symptoms, rejectionNote, ct);
            }

            var tracking = await GetTrackingByProcurementIdAsync(procurementId, ct);

            return new ProcurementTrackingResponse
            {
                Success = true,
                Message = $"Procurement dikembalikan untuk revisi ({GetProcurementStatusDescription(newStatus)}).",
                Data = tracking
            };
        }

        public async Task<ProcurementTrackingResponse> ResubmitRevisionAsync(
            string procurementId,
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

            var currentStatus = procurement.ProcurementStatus;
            var pendingSymptoms = procurement.PendingRevisionSymptoms ?? RejectionSymptom.None;

            // Validate current status is in revision
            if (currentStatus != ProcurementStatus.NeedsRevisionData &&
                currentStatus != ProcurementStatus.NeedsRevisionPR)
            {
                return new ProcurementTrackingResponse
                {
                    Success = false,
                    Message = "Procurement tidak dalam status revisi."
                };
            }

            ProcurementStatus newStatus;
            string message;

            if (currentStatus == ProcurementStatus.NeedsRevisionData)
            {
                // Clear data issue symptoms from pending
                pendingSymptoms = pendingSymptoms.GetPRIssues();
                procurement.PendingRevisionSymptoms = pendingSymptoms;

                if (pendingSymptoms.HasPRIssues())
                {
                    // Move to PR revision phase
                    newStatus = ProcurementStatus.NeedsRevisionPR;
                    message = "Revisi data selesai. Lanjut ke revisi PR/Dokumen oleh APPO.";

                    // Send notification to APPO for PR revision
                    await SendRevisionNotificationToAppo(
                        procurement,
                        pendingSymptoms,
                        procurement.RejectionNote ?? "Lanjutan revisi dari data issue",
                        ct
                    );
                }
                else
                {
                    // No more pending symptoms - complete revision
                    newStatus = ProcurementStatus.WaitingApprovalAnalyst;
                    message = "Revisi selesai. Procurement dikirim ulang ke approval Analyst HTE.";
                    CompleteRevision(procurement, submittedByUserId);
                }
            }
            else // NeedsRevisionPR
            {
                // Check for special PRCannotBeCombined symptom
                if (pendingSymptoms.HasPRCannotBeCombined())
                {
                    // Unlink and reset
                    var unlinkResult = await UnlinkAndResetProcurementAsync(procurementId, submittedByUserId, ct);
                    return unlinkResult;
                }

                // Clear PR symptoms
                procurement.PendingRevisionSymptoms = RejectionSymptom.None;

                // Complete revision - go back to OnCreateDP3 for APPO to continue
                newStatus = ProcurementStatus.OnCreateDP3;
                message = "Revisi PR/Dokumen selesai. APPO dapat melanjutkan proses.";
                CompleteRevision(procurement, submittedByUserId);
            }

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurementId,
                newStatus,
                submittedByUserId,
                $"Resubmitted after revision. Count: {procurement.RevisionCount}",
                ct
            );

            var tracking = await GetTrackingByProcurementIdAsync(procurementId, ct);

            return new ProcurementTrackingResponse
            {
                Success = true,
                Message = message,
                Data = tracking
            };
        }

        public async Task<ProcurementTrackingResponse> UnlinkAndResetProcurementAsync(
            string procurementId,
            string resetByUserId,
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

            var oldPrId = procurement.PrId;
            var oldAppoUserId = procurement.AppoUserId;

            // Unlink from PR
            procurement.PrId = null;
            procurement.AppoUserId = null;
            procurement.PickedUpAt = null;

            // Reset revision fields
            procurement.PendingRevisionSymptoms = RejectionSymptom.None;
            procurement.ResubmittedAt = DateTime.UtcNow;
            procurement.ResubmittedByUserId = resetByUserId;
            procurement.RevisionCount++;
            procurement.UpdatedAt = DateTime.UtcNow;

            // Set status back to Created (needs pickup)
            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurementId,
                ProcurementStatus.OnCreateDP3,
                resetByUserId,
                $"Unlinked from PR due to PRCannotBeCombined. Previous PR: {oldPrId}",
                ct
            );

            // Update old PR status if exists
            if (!string.IsNullOrEmpty(oldPrId))
            {
                var oldPr = await _prRepo.GetWithTrackingIncludesByPrIdAsync(oldPrId, ct);
                if (oldPr != null)
                {
                    // Check if PR still has procurements linked
                    var remainingProcs = await _procurementRepo.GetByPrIdWithTrackingAsync(oldPrId, ct);
                    if (!remainingProcs.Any())
                    {
                        // No more procurements - set PR to ReturnedFromProcurement
                        oldPr.Status = PurchaseRequisitionStatus.ReturnedFromProcurement;
                        oldPr.UpdatedAt = DateTime.UtcNow;
                        await _prRepo.UpdateAsync(oldPr, ct);
                        await _prRepo.AddStatusHistoryAsync(
                            oldPrId,
                            PurchaseRequisitionStatus.ReturnedFromProcurement,
                            resetByUserId,
                            "All procurements unlinked due to PRCannotBeCombined",
                            ct
                        );
                        await _prRepo.SaveChangesAsync(ct);
                    }
                    else
                    {
                        // Recalculate PR status based on remaining procurements
                        await RecalculatePrStatusAsync(oldPrId, ct);
                    }
                }
            }

            // Send notification to old APPO about unlink
            if (!string.IsNullOrEmpty(oldAppoUserId))
            {
                await SendUnlinkNotificationToAppo(procurement, oldAppoUserId, oldPrId, ct);
            }

            // Send notification that procurement needs pickup
            await SendProcurementNeedsPickupNotification(procurement, ct);

            var tracking = await GetTrackingByProcurementIdAsync(procurementId, ct);

            return new ProcurementTrackingResponse
            {
                Success = true,
                Message = "Procurement telah dilepas dari PR dan perlu di-pickup ulang oleh APPO.",
                Data = tracking
            };
        }

        private void CompleteRevision(Procurement procurement, string submittedByUserId)
        {
            procurement.ResubmittedAt = DateTime.UtcNow;
            procurement.ResubmittedByUserId = submittedByUserId;
            procurement.RevisionCount++;

            // Clear rejection fields but keep history
            procurement.RejectionNote = null;
            procurement.RejectedAt = null;
            procurement.RejectedByUserId = null;
            procurement.RejectionSymptoms = null;
            procurement.PendingRevisionSymptoms = null;
            procurement.StatusBeforeRejection = null;
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
                // Get CT (Final Offer) from PNL to determine required approval level
                var ct_value = await GetCtAsync(procurementId);
                var requiredLevel = ApprovalConstants.GetRequiredApprovalLevel(ct_value);
                
                var newStatus = GetNextApprovalStatus(currentStatus, requiredLevel);

                if (newStatus != currentStatus)
                {
                    // Track approval timeline (Selesai for current, Mulai for next)
                    var now = DateTime.UtcNow;
                    SetApprovalTimelineOnStatusChange(procurement, currentStatus, newStatus, now);
                    await _procurementRepo.UpdateProcurementAsync(procurement);

                    await UpdateProcurementStatusAsync(
                        procurementId,
                        newStatus,
                        approverUserId,
                        note ?? "Approved",
                        ct
                    );

                    // Send notification to the PR creator/submitter about approval progress
                    await SendApprovalProgressNotification(procurement, currentStatus, newStatus, approverUserId, ct);

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

                // Send rejection notification to the PR creator/submitter
                await SendRejectionNotification(procurement, currentStatus, approverUserId, note, ct);

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

            // Map PnL Summary if available
            PnLSummaryCardDto? pnlSummary = null;
            var profitLoss = procurement.ProfitLosses?.FirstOrDefault();
            if (profitLoss != null)
            {
                // Calculate TotalRevenue from Items
                var totalRevenue = profitLoss.Items?.Sum(i => i.Revenue) ?? 0;
                
                pnlSummary = new PnLSummaryCardDto
                {
                    ProfitLossId = profitLoss.ProfitLossId,
                    TotalRevenue = totalRevenue,
                    FinalOffer = profitLoss.SelectedVendorFinalOffer,
                    Profit = profitLoss.Profit,
                    ProfitMarginPercent = profitLoss.ProfitPercent,
                    SelectedVendorName = profitLoss.SelectedVendor?.VendorName
                };
            }

            return new ProcurementTrackingDto
            {
                ProcurementId = procurement.ProcurementId,
                ProcNum = procurement.ProcNum ?? string.Empty,
                Wonum = procurement.Wonum,
                JobName = procurement.JobName ?? string.Empty,
                DocumentDate = procurement.DocumentDate,
                CurrentStatus = procurement.ProcurementStatus,
                CurrentStatusDescription = GetProcurementStatusDescription(procurement.ProcurementStatus),
                ProjectRegion = procurement.ProjectRegion,
                PrId = procurement.PrId,
                PrNumber = procurement.PurchaseRequisition?.PrNumber,
                IspaNumber = procurement.IspaNumber,
                IspaSubmittedAt = procurement.IspaSubmittedAt,
                IspaSubmittedByUserName = procurement.IspaSubmittedByUser?.FullName,
                IspaDate = procurement.IspaDate,
                IspaSubmitDate = procurement.IspaSubmitDate,
                IspaFileName = procurement.IspaFileName,
                IspaFileObjectKey = procurement.IspaFileObjectKey,
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
                // Responsible Users / Approvers
                AnalystHteUserName = procurement.AnalystHteUser?.FullName,
                AssistantManagerUserName = procurement.AssistantManagerUser?.FullName,
                ManagerUserName = procurement.ManagerUser?.FullName,
                VicePresidentUserName = procurement.VicePresidentUser?.FullName,
                OperationDirectorUserName = procurement.OperationDirectorUser?.FullName,
                PresidentDirectorUserName = procurement.PresidentDirectorUser?.FullName,
                // Pjs (Acting) Flags
                AnalystHtePjs = procurement.AnalystHtePjs,
                AssistantManagerPjs = procurement.AssistantManagerPjs,
                ManagerPjs = procurement.ManagerPjs,
                VicePresidentPjs = procurement.VicePresidentPjs,
                OperationDirectorPjs = procurement.OperationDirectorPjs,
                PresidentDirectorPjs = procurement.PresidentDirectorPjs,
                // Contract Total (Final Offer PNL) and Required Approval Level
                FinalOfferPnl = pnlSummary?.FinalOffer,
                RequiredApprovalLevel = Constants.ApprovalConstants.GetRequiredApprovalLevel(pnlSummary?.FinalOffer ?? 0),
                StatusHistory = procurement.StatusHistories?.Select(MapToHistoryDto).ToList() ?? new List<ProcurementStatusHistoryDto>(),
                TotalMandatoryDocs = totalDocs,
                UploadedMandatoryDocs = uploadedDocs,
                TotalDocuments = procurement.ProcDocuments?.Count ?? 0,
                ApprovalQrUrl = !string.IsNullOrEmpty(procurement.ApprovalToken)
                    ? GenerateQrCodeDataUri(procurement.ApprovalToken)
                    : null,
                NextApproverRole = GetNextApproverRole(procurement.ProcurementStatus),
                NextApproverName = GetNextApproverName(procurement),
                PnLSummary = pnlSummary,
                // Revision Tracking Fields
                RejectionSymptoms = procurement.RejectionSymptoms,
                PendingRevisionSymptoms = procurement.PendingRevisionSymptoms,
                StatusBeforeRejection = procurement.StatusBeforeRejection,
                RevisionCount = procurement.RevisionCount,
                ResubmittedAt = procurement.ResubmittedAt,
                ResubmittedByUserName = procurement.ResubmittedByUser?.FullName,
                // PIC Ops Info
                PicOpsUserId = procurement.PicOpsUserId,
                PicOpsUserName = procurement.PicOpsUser?.FullName
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
                ProcurementStatus.WaitingApprovalVP => "Vice President",
                ProcurementStatus.WaitingApprovalOpDir => "Operation Director",
                ProcurementStatus.WaitingApprovalPresDir => "President Director",
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
                ProcurementStatus.WaitingApprovalVP => procurement.VicePresidentUserId,
                ProcurementStatus.WaitingApprovalOpDir => procurement.OperationDirectorUserId,
                ProcurementStatus.WaitingApprovalPresDir => procurement.PresidentDirectorUserId,
                _ => null
            };
        }

        /// <summary>
        /// Get CT (Contract Total) from ProfitLoss - uses SelectedVendorFinalOffer
        /// </summary>
        private async Task<decimal> GetCtAsync(string procurementId)
        {
            var procurement = await _procurementRepo.GetByIdAsync(procurementId);
            if (procurement == null)
                return 0m;

            var profitLoss = procurement.ProfitLosses?.FirstOrDefault();
            if (profitLoss == null)
                return 0m;

            return profitLoss.SelectedVendorFinalOffer;
        }

        /// <summary>
        /// Determine next approval status based on current status and required approval level
        /// </summary>
        private static ProcurementStatus GetNextApprovalStatus(ProcurementStatus currentStatus, int requiredLevel)
        {
            return currentStatus switch
            {
                // Standard flow (always present)
                ProcurementStatus.WaitingApprovalAnalyst => ProcurementStatus.WaitingApprovalAsstManager,
                ProcurementStatus.WaitingApprovalAsstManager => ProcurementStatus.WaitingApprovalManager,
                
                // Manager -> VP or ISPA based on required level
                ProcurementStatus.WaitingApprovalManager => requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_VP
                    ? ProcurementStatus.WaitingApprovalVP
                    : ProcurementStatus.OnSubmitISPA,
                
                // VP -> OpDir or ISPA based on required level
                ProcurementStatus.WaitingApprovalVP => requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_OP_DIR
                    ? ProcurementStatus.WaitingApprovalOpDir
                    : ProcurementStatus.OnSubmitISPA,
                
                // OpDir -> PresDir or ISPA based on required level
                ProcurementStatus.WaitingApprovalOpDir => requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_PRES_DIR
                    ? ProcurementStatus.WaitingApprovalPresDir
                    : ProcurementStatus.OnSubmitISPA,
                
                // PresDir -> ISPA (final approval)
                ProcurementStatus.WaitingApprovalPresDir => ProcurementStatus.OnSubmitISPA,
                
                _ => currentStatus
            };
        }

        /// <summary>
        /// Set approval timeline fields when status changes (Selesai for current approver, Mulai for next)
        /// </summary>
        private static void SetApprovalTimelineOnStatusChange(
            Procurement procurement, 
            ProcurementStatus fromStatus, 
            ProcurementStatus toStatus, 
            DateTime timestamp)
        {
            // Set "Selesai" for the approver who just approved
            switch (fromStatus)
            {
                case ProcurementStatus.WaitingApprovalManager:
                    procurement.ManagerApprovalEndAt = timestamp;
                    break;
                case ProcurementStatus.WaitingApprovalVP:
                    procurement.VpApprovalEndAt = timestamp;
                    break;
                case ProcurementStatus.WaitingApprovalOpDir:
                    procurement.OpDirApprovalEndAt = timestamp;
                    break;
                case ProcurementStatus.WaitingApprovalPresDir:
                    procurement.PresDirApprovalEndAt = timestamp;
                    break;
            }

            // Set "Mulai" for the next approver
            switch (toStatus)
            {
                case ProcurementStatus.WaitingApprovalManager:
                    procurement.ManagerApprovalStartAt ??= timestamp;
                    break;
                case ProcurementStatus.WaitingApprovalVP:
                    procurement.VpApprovalStartAt ??= timestamp;
                    break;
                case ProcurementStatus.WaitingApprovalOpDir:
                    procurement.OpDirApprovalStartAt ??= timestamp;
                    break;
                case ProcurementStatus.WaitingApprovalPresDir:
                    procurement.PresDirApprovalStartAt ??= timestamp;
                    break;
            }
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
                ProcurementStatus.WaitingApprovalVP => procurement.VicePresidentUserId,
                ProcurementStatus.WaitingApprovalOpDir => procurement.OperationDirectorUserId,
                ProcurementStatus.WaitingApprovalPresDir => procurement.PresidentDirectorUserId,
                _ => null
            };

            if (string.IsNullOrEmpty(approverUserId))
                return;

            await _notificationService.SendNotificationAsync(
                userId: approverUserId,
                title: $"Procurement {procurement.ProcNum} menunggu approval Anda",
                message: $"WO: {procurement.Wonum} - {procurement.JobName}",
                notificationType: "ApprovalRequest",
                actionUrl: $"/ProcurementTracking/Details/{procurement.ProcurementId}",
                referenceId: procurement.ProcurementId,
                createdByUserId: procurement.ApprovalSentByUserId,
                ct: ct
            );

            // Broadcast approval badge update to the approver
            // We send a placeholder count - the client will fetch the actual count
            await _notificationPusher.PushApprovalBadgeAsync(approverUserId, -1);
        }

        private async Task SendApprovalProgressNotification(
            Procurement procurement,
            ProcurementStatus previousStatus,
            ProcurementStatus newStatus,
            string? approverUserId,
            CancellationToken ct
        )
        {
            // Get the user who submitted the approval request (Operation user)
            var submitterUserId = procurement.ApprovalSentByUserId;
            if (string.IsNullOrEmpty(submitterUserId))
                return;

            // Don't send notification to the approver themselves if they are also the submitter
            if (submitterUserId == approverUserId)
                return;

            var approverRole = previousStatus switch
            {
                ProcurementStatus.WaitingApprovalAnalyst => "Analyst HTE",
                ProcurementStatus.WaitingApprovalAsstManager => "Assistant Manager HTE",
                ProcurementStatus.WaitingApprovalManager => "Manager Transport & Logistic",
                ProcurementStatus.WaitingApprovalVP => "Vice President",
                ProcurementStatus.WaitingApprovalOpDir => "Operation Director",
                ProcurementStatus.WaitingApprovalPresDir => "President Director",
                _ => "Approver"
            };

            var nextStep = newStatus switch
            {
                ProcurementStatus.WaitingApprovalAsstManager => "Menunggu approval Assistant Manager",
                ProcurementStatus.WaitingApprovalManager => "Menunggu approval Manager",
                ProcurementStatus.WaitingApprovalVP => "Menunggu approval Vice President",
                ProcurementStatus.WaitingApprovalOpDir => "Menunggu approval Operation Director",
                ProcurementStatus.WaitingApprovalPresDir => "Menunggu approval President Director",
                ProcurementStatus.OnSubmitISPA => "Semua approval selesai, siap submit ISPA",
                _ => "Proses berlanjut"
            };

            await _notificationService.SendNotificationAsync(
                userId: submitterUserId,
                title: $"Procurement {procurement.ProcNum} telah di-approve oleh {approverRole}",
                message: $"WO: {procurement.Wonum} - {nextStep}",
                notificationType: $"ApprovedBy{approverRole.Replace(" ", "")}",
                actionUrl: $"/ProcurementTracking/Details/{procurement.ProcurementId}",
                referenceId: procurement.ProcurementId,
                createdByUserId: approverUserId,
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

        private async Task SendRejectionNotification(
            Procurement procurement,
            ProcurementStatus rejectedAtStatus,
            string? rejectorUserId,
            string? rejectionNote,
            CancellationToken ct
        )
        {
            // Get the user who submitted the approval request (Operation user)
            var submitterUserId = procurement.ApprovalSentByUserId;
            if (string.IsNullOrEmpty(submitterUserId))
                return;

            var rejectorRole = rejectedAtStatus switch
            {
                ProcurementStatus.WaitingApprovalAnalyst => "Analyst HTE",
                ProcurementStatus.WaitingApprovalAsstManager => "Assistant Manager HTE",
                ProcurementStatus.WaitingApprovalManager => "Manager Transport & Logistic",
                ProcurementStatus.WaitingApprovalVP => "Vice President",
                ProcurementStatus.WaitingApprovalOpDir => "Operation Director",
                ProcurementStatus.WaitingApprovalPresDir => "President Director",
                _ => "Approver"
            };

            var message = string.IsNullOrEmpty(rejectionNote)
                ? $"WO: {procurement.Wonum} - Procurement ditolak"
                : $"WO: {procurement.Wonum} - Alasan: {rejectionNote}";

            await _notificationService.SendNotificationAsync(
                userId: submitterUserId,
                title: $"Procurement {procurement.ProcNum} ditolak oleh {rejectorRole}",
                message: message,
                notificationType: "PrRejected",
                actionUrl: $"/ProcurementTracking/Details/{procurement.ProcurementId}",
                referenceId: procurement.ProcurementId,
                createdByUserId: rejectorUserId,
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

        /// <inheritdoc/>
        public async Task<string?> GetIspaFileUrlAsync(string procurementId, CancellationToken ct = default)
        {
            var procurement = await _procurementRepo.GetByIdAsync(procurementId);
            if (procurement == null || string.IsNullOrEmpty(procurement.IspaFileObjectKey))
                return null;

            // Get presigned URL from MinIO (valid for 1 hour)
            return await _objectStorage.GetPresignedUrlAsync(
                _storageOptions.Bucket,
                procurement.IspaFileObjectKey,
                TimeSpan.FromHours(1),
                ct
            );
        }

        #endregion

        #region Revision Notification Helpers

        private async Task SendRevisionNotificationToPicOps(
            Procurement procurement,
            RejectionSymptom symptoms,
            string rejectionNote,
            CancellationToken ct
        )
        {
            // Notify PIC Operations user
            var picOpsUserId = procurement.PicOpsUserId;
            if (string.IsNullOrEmpty(picOpsUserId))
            {
                _logger.LogWarning(
                    "Cannot send revision notification: PicOpsUserId is empty for procurement {ProcurementId}",
                    procurement.ProcurementId
                );
                return;
            }

            var symptomNames = string.Join(", ", symptoms.GetDataIssues().GetSelectedDisplayNames());

            await _notificationService.SendNotificationAsync(
                userId: picOpsUserId,
                title: $"Procurement {procurement.ProcNum} perlu revisi data",
                message: $"WO: {procurement.Wonum}\nIssue: {symptomNames}\nCatatan: {rejectionNote}",
                notificationType: "NeedsRevisionData",
                actionUrl: $"/Procurements/Edit/{procurement.ProcurementId}",
                referenceId: procurement.ProcurementId,
                createdByUserId: procurement.RejectedByUserId,
                ct: ct
            );
        }

        private async Task SendRevisionNotificationToAppo(
            Procurement procurement,
            RejectionSymptom symptoms,
            string rejectionNote,
            CancellationToken ct
        )
        {
            // Notify APPO user who picked up this procurement
            var appoUserId = procurement.AppoUserId;
            if (string.IsNullOrEmpty(appoUserId))
            {
                _logger.LogWarning(
                    "Cannot send revision notification: AppoUserId is empty for procurement {ProcurementId}",
                    procurement.ProcurementId
                );
                return;
            }

            var symptomNames = string.Join(", ", symptoms.GetPRIssues().GetSelectedDisplayNames());
            var hasUnlinkSymptom = symptoms.HasPRCannotBeCombined();

            var message = hasUnlinkSymptom
                ? $"WO: {procurement.Wonum}\n⚠️ PERLU DILEPAS DARI PR\nIssue: {symptomNames}\nCatatan: {rejectionNote}"
                : $"WO: {procurement.Wonum}\nIssue: {symptomNames}\nCatatan: {rejectionNote}";

            await _notificationService.SendNotificationAsync(
                userId: appoUserId,
                title: $"Procurement {procurement.ProcNum} perlu revisi PR/Dokumen",
                message: message,
                notificationType: "NeedsRevisionPR",
                actionUrl: $"/ProcurementTracking/Details/{procurement.ProcurementId}",
                referenceId: procurement.ProcurementId,
                createdByUserId: procurement.RejectedByUserId,
                ct: ct
            );
        }

        private async Task SendUnlinkNotificationToAppo(
            Procurement procurement,
            string appoUserId,
            string? oldPrId,
            CancellationToken ct
        )
        {
            await _notificationService.SendNotificationAsync(
                userId: appoUserId,
                title: $"Procurement {procurement.ProcNum} dilepas dari PR",
                message: $"WO: {procurement.Wonum}\nProcurement telah dilepas dari PR {oldPrId} karena tidak bisa digabung.\nProcurement ini perlu di-pickup ulang.",
                notificationType: "ProcurementUnlinked",
                actionUrl: $"/ProcurementTracking/Details/{procurement.ProcurementId}",
                referenceId: procurement.ProcurementId,
                createdByUserId: procurement.ResubmittedByUserId,
                ct: ct
            );
        }

        private async Task SendProcurementNeedsPickupNotification(
            Procurement procurement,
            CancellationToken ct
        )
        {
            // Send to all APPO users that a procurement needs pickup
            // This is a broadcast notification
            await _notificationService.SendNotificationToRoleAsync(
                roleName: "AP-PO",
                title: $"Procurement perlu di-pickup",
                message: $"WO: {procurement.Wonum}\nProcurement {procurement.ProcNum} tersedia untuk di-pickup.\n(Revisi ke-{procurement.RevisionCount})",
                notificationType: "ProcurementNeedsPickup",
                actionUrl: $"/Procurements/Details/{procurement.ProcurementId}",
                referenceId: procurement.ProcurementId,
                createdByUserId: null,
                ct: ct
            );
        }

        #endregion
    }
}
