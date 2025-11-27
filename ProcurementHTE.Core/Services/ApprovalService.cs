using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs; // GateInfoDto, RoleInfoDto
using static ProcurementHTE.Core.Utils.ApprovalServiceHelper;

namespace ProcurementHTE.Core.Services
{
    public class ApprovalService : IApprovalService
    {
        private readonly IApprovalRepository _approvalRepository;
        private readonly IProcurementService _woService;

        public ApprovalService(
            IApprovalRepository approvalRepository,
            IProcurementService woService
        )
        {
            _approvalRepository = approvalRepository;
            _woService = woService;
        }

        // ===== existing =====
        public Task<IReadOnlyList<ProcDocumentApprovals>> GetPendingApprovalsForUserAsync(
            User user
        ) => _approvalRepository.GetPendingApprovalsForUserAsync(user);

        public async Task ApproveAsync(string approvalId, string approverUserId)
        {
            var result = await _approvalRepository.ApproveAsync(approvalId, approverUserId);
            if (result.AllDocsApproved)
                await _woService.MarkAsCompletedAsync(result.ProcurementId);
        }

        public Task RejectAsync(string approvalId, string approverUserId, string? note) =>
            _approvalRepository.RejectAsync(approvalId, approverUserId, note);

        public Task<GateInfoDto?> GetCurrentPendingGateByQrAsync(
            string qrText,
            CancellationToken ct = default
        ) => _approvalRepository.GetCurrentPendingGateByQrAsync(qrText, ct);

        public Task<GateInfoDto?> GetCurrentPendingGateByApprovalIdAsync(
            string procDocumentApprovalId,
            CancellationToken ct = default
        ) => _approvalRepository.GetCurrentPendingGateByApprovalIdAsync(procDocumentApprovalId, ct);

        // ===== NEW: high-level =====
        public async Task<ApprovalUpdateResult> UpdateStatusByQrAsync(
            string qrText,
            string action,
            string? note,
            User currentUser,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(qrText))
                return Fail("InvalidAction", "QrText wajib diisi.");

            if (!NormalizeAction(action, out var normalized))
                return Fail("InvalidAction", "Action harus 'approve' atau 'reject'.");

            var gate = await GetCurrentPendingGateByQrAsync(qrText, ct);
            if (gate == null)
                return Fail("QrNotFound", "QR tidak cocok dokumen manapun.");

            if (gate.RequiredRoles.Count == 0)
                return await BuildFinalStateResponseAsync(_approvalRepository, gate, ct);

            var userRoleNames = await _approvalRepository.GetUserRoleNamesAsync(currentUser.Id, ct);

            var roleHit = gate.RequiredRoles.FirstOrDefault(r =>
            {
                var roleMatches =
                    !string.IsNullOrWhiteSpace(r.RoleName)
                    && userRoleNames.Contains(r.RoleName!, StringComparer.OrdinalIgnoreCase);
                if (!roleMatches)
                    return false;

                if (
                    !string.IsNullOrWhiteSpace(r.ApproverId)
                    && !string.Equals(r.ApproverId, currentUser.Id, StringComparison.Ordinal)
                )
                    return false;

                return true;
            });

            if (roleHit == null)
                return await BuildRoleValidationFailAsync(
                    _approvalRepository,
                    gate,
                    currentUser,
                    userRoleNames,
                    ct
                );

            var approvalId = roleHit.ProcDocumentApprovalId!;
            try
            {
                if (normalized == "approve")
                    await ApproveAsync(approvalId, currentUser.Id);
                else
                    await RejectAsync(approvalId, currentUser.Id, note);

                return new ApprovalUpdateResult
                {
                    Ok = true,
                    Action = normalized,
                    ApprovalId = approvalId,
                    ProcurementId = gate.ProcurementId,
                    ProcDocumentId = gate.ProcDocumentId,
                    When = DateTime.UtcNow,
                };
            }
            catch (InvalidOperationException ex)
            {
                return Fail("Blocked", ex.Message);
            }
            catch (Exception ex)
            {
                return Fail("Error", ex.Message);
            }
        }

        public async Task<ApprovalUpdateResult> UpdateStatusByApprovalIdAsync(
            string approvalId,
            string action,
            string? note,
            User currentUser,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(approvalId))
                return Fail("InvalidAction", "ProcDocumentApprovalId wajib diisi.");

            if (!NormalizeAction(action, out var normalized))
                return Fail("InvalidAction", "Action harus 'approve' atau 'reject'.");

            var gate = await GetCurrentPendingGateByApprovalIdAsync(approvalId, ct);
            if (gate == null)
                return Fail("ApprovalNotFound", "Approval/dokumen tidak ditemukan.");

            if (gate.RequiredRoles.Count == 0)
                return await BuildFinalStateResponseAsync(_approvalRepository, gate, ct);

            var gateTarget = gate.RequiredRoles.FirstOrDefault(r =>
                r.ProcDocumentApprovalId == approvalId
            );
            if (gateTarget == null)
            {
                return new ApprovalUpdateResult
                {
                    Ok = false,
                    Reason = "Blocked",
                    Message = "Approval masih terblokir oleh step sebelumnya.",
                    ProcurementId = gate.ProcurementId,
                    ProcDocumentId = gate.ProcDocumentId,
                    CurrentGateLevel = gate.Level,
                    CurrentGateSequence = gate.SequenceOrder,
                    RequiredRoles = gate.RequiredRoles,
                };
            }

            var userRoleNames = await _approvalRepository.GetUserRoleNamesAsync(currentUser.Id, ct);

            bool matchRole = false;
            if (!string.IsNullOrWhiteSpace(gateTarget.RoleName))
                matchRole = userRoleNames.Contains(
                    gateTarget.RoleName!,
                    StringComparer.OrdinalIgnoreCase
                );

            if (!matchRole && !string.IsNullOrWhiteSpace(gateTarget.RoleId))
            {
                var userRoleIds = await _approvalRepository.GetUserRoleIdsAsync(currentUser.Id, ct);
                matchRole = userRoleIds.Contains(gateTarget.RoleId!);
            }

            if (!matchRole)
                return await BuildRoleValidationFailAsync(
                    _approvalRepository,
                    gate,
                    currentUser,
                    userRoleNames,
                    ct
                );

            try
            {
                if (normalized == "approve")
                    await ApproveAsync(approvalId, currentUser.Id);
                else
                    await RejectAsync(approvalId, currentUser.Id, note);

                return new ApprovalUpdateResult
                {
                    Ok = true,
                    Action = normalized,
                    ApprovalId = approvalId,
                    When = DateTime.UtcNow,
                };
            }
            catch (InvalidOperationException ex)
            {
                return Fail("Blocked", ex.Message);
            }
            catch (Exception ex)
            {
                return Fail("Error", ex.Message);
            }
        }

        public async Task<ApprovalUpdateResult> UpdateStatusByDocumentIdAsync(
            string procDocumentId,
            string action,
            string? note,
            User currentUser,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(procDocumentId))
                return Fail("InvalidAction", "ProcDocumentId wajib diisi.");

            if (!NormalizeAction(action, out var normalized))
                return Fail("InvalidAction", "Action harus 'approve' atau 'reject'.");

            var gate = await _approvalRepository.GetCurrentPendingGateByDocumentIdAsync(
                procDocumentId,
                ct
            );
            if (gate == null)
                return Fail("DocumentNotFound", "Dokumen tidak ditemukan.");

            if (gate.RequiredRoles.Count == 0)
                return await BuildFinalStateResponseAsync(_approvalRepository, gate, ct);

            var userRoleNames = await _approvalRepository.GetUserRoleNamesAsync(currentUser.Id, ct);

            var roleHit = gate.RequiredRoles.FirstOrDefault(r =>
            {
                var roleMatches =
                    !string.IsNullOrWhiteSpace(r.RoleName)
                    && userRoleNames.Contains(r.RoleName!, StringComparer.OrdinalIgnoreCase);
                if (!roleMatches)
                    return false;

                if (
                    !string.IsNullOrWhiteSpace(r.ApproverId)
                    && !string.Equals(r.ApproverId, currentUser.Id, StringComparison.Ordinal)
                )
                    return false;

                return true;
            });

            if (roleHit == null)
                return await BuildRoleValidationFailAsync(
                    _approvalRepository,
                    gate,
                    currentUser,
                    userRoleNames,
                    ct
                );

            var approvalId = roleHit.ProcDocumentApprovalId!;
            try
            {
                if (normalized == "approve")
                    await ApproveAsync(approvalId, currentUser.Id);
                else
                    await RejectAsync(approvalId, currentUser.Id, note);

                return new ApprovalUpdateResult
                {
                    Ok = true,
                    Action = normalized,
                    ApprovalId = approvalId,
                    ProcurementId = gate.ProcurementId,
                    ProcDocumentId = gate.ProcDocumentId,
                    When = DateTime.UtcNow,
                };
            }
            catch (InvalidOperationException ex)
            {
                return Fail("Blocked", ex.Message);
            }
            catch (Exception ex)
            {
                return Fail("Error", ex.Message);
            }
        }

        // ApprovalService.cs (tambahkan method ringkas)
        public async Task<ApprovalTimelineDto?> GetApprovalTimelineAsync(
            string procDocumentId,
            CancellationToken ct = default
        )
        {
            var gate = await _approvalRepository.GetCurrentPendingGateByDocumentIdAsync(
                procDocumentId,
                ct
            );
            if (gate == null)
                return null;

            var chain = await _approvalRepository.GetDocumentApprovalChainAsync(procDocumentId, ct);

            return new ApprovalTimelineDto
            {
                ProcurementId = gate.ProcurementId!,
                ProcDocumentId = gate.ProcDocumentId!,
                DocStatus = gate.DocStatus,
                CurrentGateLevel = gate.Level,
                CurrentGateSequence = gate.SequenceOrder,
                RequiredRoles = gate.RequiredRoles, // List<RoleInfoDto>
                Steps = chain
                    .Select(c => new ApprovalStepDto
                    {
                        Level = c.Level,
                        SequenceOrder = c.SequenceOrder,
                        RoleName = c.RoleName,
                        Status = c.Status,
                        AssignedApproverUserId = c.AssignedApproverUserId,
                        AssignedApproverFullName = c.AssignedApproverFullName,
                        ApproverUserId = c.ApproverUserId,
                        ApproverFullName = c.ApproverFullName,
                        ApprovedAt = c.ApprovedAt,
                        Note = c.Note,
                    })
                    .ToList(),
            };
        }
    }
}
