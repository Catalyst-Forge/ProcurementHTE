using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Utils
{
    public static class ApprovalServiceHelper
    {
        // --- tiny helpers ---
        public static bool NormalizeAction(string? action, out string normalized)
        {
            normalized = (action ?? "").Trim().ToLowerInvariant();
            return normalized is "approve" or "reject";
        }

        public static ApprovalUpdateResult Fail(string reason, string message)
            => new() { Ok = false, Reason = reason, Message = message };

        // --- final-state response (termasuk rejected detail) ---
        public static async Task<ApprovalUpdateResult> BuildFinalStateResponseAsync(
            IApprovalRepository repo,
            GateInfoDto gate,
            CancellationToken ct)
        {
            bool isRejected = string.Equals(gate.DocStatus, "Rejected", StringComparison.OrdinalIgnoreCase);

            var res = new ApprovalUpdateResult
            {
                Ok = false,
                Reason = isRejected ? "AlreadyRejected" : "AlreadyFinalized",
                Message = isRejected
                    ? "Dokumen sudah ditolak (Rejected), tidak ada approval PENDING."
                    : "Dokumen ini tidak memiliki approval PENDING.",
                ProcurementId = gate.ProcurementId,
                ProcDocumentId = gate.ProcDocumentId,
                DocStatus = gate.DocStatus,
                CurrentGateLevel = null,
                CurrentGateSequence = null,
                RequiredRoles = new(),
                YourRoles = new(),
            };

            if (isRejected && !string.IsNullOrWhiteSpace(gate.ProcDocumentId))
            {
                var info = await repo.GetLastRejectionInfoAsync(gate.ProcDocumentId, ct);
                if (info != null)
                {
                    res.RejectedByUserId = info.RejectedByUserId;
                    res.RejectedByUserName = info.RejectedByUserName;
                    res.RejectedByFullName = info.RejectedByFullName;
                    res.RejectedAt = info.RejectedAt;
                    res.RejectNote = info.RejectNote;
                }
            }

            return res;
        }

        // --- NotYourTurn builder (lengkap dgn last approval by user) ---
        public static async Task<ApprovalUpdateResult> BuildNotYourTurnAsync(
            IApprovalRepository repo,
            GateInfoDto gate,
            User currentUser,
            IEnumerable<string> userRoleNames,
            CancellationToken ct)
        {
            var last = await repo.GetLastApprovalByUserOnDocumentAsync(currentUser.Id, gate.ProcDocumentId!, ct);

            return new ApprovalUpdateResult
            {
                Ok = false,
                Reason = "NotYourTurn",
                Message = "Bukan giliran role Anda untuk approve.",
                ProcurementId = gate.ProcurementId,
                ProcDocumentId = gate.ProcDocumentId,
                DocStatus = gate.DocStatus,
                CurrentGateLevel = gate.Level,
                CurrentGateSequence = gate.SequenceOrder,
                RequiredRoles = gate.RequiredRoles,
                YourRoles = userRoleNames.ToList(),
                AlreadyApprovedByYou = last != null,
                YourLastApprovalLevel = last?.Level,
                YourLastApprovalSequence = last?.SequenceOrder,
                YourLastApprovalAt = last?.ApprovedAt,
            };
        }

        // --- Validasi ketika user tidak match dgn gate saat ini ---
        public static async Task<ApprovalUpdateResult> BuildRoleValidationFailAsync(
            IApprovalRepository repo,
            GateInfoDto gate,
            User currentUser,
            IEnumerable<string> userRoleNames,
            CancellationToken ct)
        {
            // 1) cek role yg dikonfig (ada Id/Name) bener-bener exist di sistem
            var reqIds = gate.RequiredRoles.Select(r => r.RoleId).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToArray();
            var reqNames = gate.RequiredRoles.Select(r => r.RoleName).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToArray();

            var existIds = await repo.GetExistingRoleIdsAsync(reqIds, ct);
            var existNames = await repo.GetExistingRoleNamesAsync(reqNames, ct);

            var noneExist =
                (reqIds.Length > 0 && existIds.Count == 0) &&
                (reqNames.Length > 0 && existNames.Count == 0);

            if (noneExist)
            {
                return new ApprovalUpdateResult
                {
                    Ok = false,
                    Reason = "InvalidGateConfig",
                    Message = "Role pada gate tidak ditemukan di sistem (periksa RoleId/RoleName).",
                    ProcurementId = gate.ProcurementId,
                    ProcDocumentId = gate.ProcDocumentId,
                    DocStatus = gate.DocStatus,
                    CurrentGateLevel = gate.Level,
                    CurrentGateSequence = gate.SequenceOrder,
                    RequiredRoles = gate.RequiredRoles,
                    YourRoles = userRoleNames.ToList(),
                };
            }

            // 2) ada rolenya, tapi ada user pemilik role tsb atau tidak?
            var eligibleCount = await repo.CountUsersWithAnyRoleAsync(reqIds, reqNames, ct);
            if (eligibleCount == 0)
            {
                return new ApprovalUpdateResult
                {
                    Ok = false,
                    Reason = "NoEligibleApprover",
                    Message = "Tidak ada user yang memiliki role yang diminta pada gate ini.",
                    ProcurementId = gate.ProcurementId,
                    ProcDocumentId = gate.ProcDocumentId,
                    DocStatus = gate.DocStatus,
                    CurrentGateLevel = gate.Level,
                    CurrentGateSequence = gate.SequenceOrder,
                    RequiredRoles = gate.RequiredRoles,
                    YourRoles = userRoleNames.ToList(),
                };
            }

            // 3) role user ada di chain dokumen ini?
            var chain = await repo.GetDocumentApprovalChainAsync(gate.ProcDocumentId!, ct);

            var appearsInDoc = chain.Any(c =>
                !string.IsNullOrWhiteSpace(c.RoleName) &&
                userRoleNames.Contains(c.RoleName!, StringComparer.OrdinalIgnoreCase)
            );

            if (appearsInDoc)
            {
                // user ini memang bagian dari chain dokumen, tapi bukan gate yang sedang aktif
                return await BuildNotYourTurnAsync(repo, gate, currentUser, userRoleNames, ct);
            }

            // 4) role user tidak ada di chain dokumen sama sekali
            return new ApprovalUpdateResult
            {
                Ok = false,
                Reason = "RoleNotInGate",
                Message = "Role Anda tidak terdaftar dalam rules approval pada dokumen ini.",
                ProcurementId = gate.ProcurementId,
                ProcDocumentId = gate.ProcDocumentId,
                DocStatus = gate.DocStatus,
                CurrentGateLevel = gate.Level,
                CurrentGateSequence = gate.SequenceOrder,
                RequiredRoles = gate.RequiredRoles,
                YourRoles = userRoleNames.ToList(),
            };
        }
    }
}
