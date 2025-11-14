using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class ApprovalRepository : IApprovalRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ApprovalRepository> _logger;

        public ApprovalRepository(AppDbContext context, ILogger<ApprovalRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ProcDocumentApprovals>> GetPendingApprovalsForUserAsync(
            User user
        )
        {
            var roles = await _context
                .UserRoles.Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            return await _context
                .ProcDocumentApprovals.Include(a => a.ProcDocument)
                .ThenInclude(d => d.DocumentType)
                .Include(a => a.Procurement)
                .Include(a => a.Role)
                .Where(a => a.Status == "Pending" && roles.Contains(a.RoleId))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool AllDocsApproved, string ProcurementId)> ApproveAsync(
            string approvalId,
            string approverUserId
        )
        {
            // Load lengkap: ProcDocument + Approvals + Procurement
            var approval = await _context
                .ProcDocumentApprovals.Include(a => a.ProcDocument)!
                .ThenInclude(d => d.Approvals)
                .Include(a => a.ProcDocument)!
                .ThenInclude(d => d.Procurement)
                .FirstOrDefaultAsync(a => a.ProcDocumentApprovalId == approvalId);

            if (approval == null)
                throw new InvalidOperationException("Approval tidak ditemukan.");

            var ProcurementEntity =
                approval.ProcDocument?.Procurement
                ?? throw new InvalidOperationException("Procurement Order tidak ditemukan.");
            var ProcurementId = ProcurementEntity.ProcurementId;

            // Idempotent: kalau sudah final, anggap OK tetapi tidak memajukan apa pun
            if (approval.Status is "Approved" or "Rejected")
                return (false, ProcurementId);

            // Harus ada pending di dokumen ini
            var docEntity =
                approval.ProcDocument
                ?? throw new InvalidOperationException("Dokumen tidak ditemukan.");
            var pendings = (docEntity.Approvals ?? []).Where(a => a.Status == "Pending").ToList();
            if (pendings.Count == 0)
                throw new InvalidOperationException("Dokumen sudah final (no pending).");

            // Gate check (level & sequence terendah)
            var minLevel = pendings.Min(a => a.Level);
            var minSeq = pendings.Where(a => a.Level == minLevel).Min(a => a.SequenceOrder);

            var isInGate = (approval.Level == minLevel) && (approval.SequenceOrder == minSeq);
            if (!isInGate)
                throw new InvalidOperationException(
                    "Approval masih terblokir oleh step sebelumnya (Blocked)."
                );

            // Status yang valid untuk dieksekusi hanya Pending
            if (!string.Equals(approval.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Approval tidak berada dalam status Pending.");

            // (Opsional) PnL threshold VP (kamu log aja, belum dipakai memengaruhi gate)
            const decimal ThresholdVP = 500_000_000m;
            var pnl = await _context
                .ProfitLosses.AsNoTracking()
                .Where(p => p.ProcurementId == ProcurementId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
            var totalOffer = pnl?.SelectedVendorFinalOffer ?? 0m;
            var needVP = totalOffer > ThresholdVP;

            _logger.LogInformation(
                "[Approve] Procurement={Procurement}, TotalOffer={TotalOffer:N0}, NeedVP={NeedVP}",
                ProcurementEntity.ProcNum,
                totalOffer,
                needVP
            );

            // 1) Set current approved
            approval.Status = "Approved";
            approval.ApproverId = approverUserId;
            approval.ApprovedAt = DateTime.UtcNow;

            var docId = approval.ProcDocumentId;
            var currentLevel = approval.Level;
            var currentSeq = approval.SequenceOrder;

            // 2) Ambil semua step untuk dokumen ini (tracking aktif)
            var all = await _context
                .ProcDocumentApprovals.Where(a => a.ProcDocumentId == docId)
                .ToListAsync();

            // Jika ada yang Rejected sebelumnya, hentikan (dok sudah mental)
            if (all.Any(x => x.Status == "Rejected"))
            {
                await _context.SaveChangesAsync();
                return (false, ProcurementId);
            }

            // 3) Apakah masih ada sequence berikutnya pada level yang sama?
            var nextSeqOnSameLevel = all.Where(x =>
                    x.Level == currentLevel && x.SequenceOrder > currentSeq && x.Status == "Blocked"
                )
                .Select(x => x.SequenceOrder)
                .DefaultIfEmpty()
                .Min();

            if (nextSeqOnSameLevel != 0) // ada sequence berikutnya
            {
                // ? Promote SEMUA step pada sequence berikutnya menjadi Pending
                foreach (
                    var step in all.Where(x =>
                        x.Level == currentLevel
                        && x.SequenceOrder == nextSeqOnSameLevel
                        && x.Status == "Blocked"
                    )
                )
                    step.Status = "Pending";

                await _context.SaveChangesAsync();
                return (false, ProcurementId);
            }

            // 4) Kalau semua step pada level saat ini sudah approved, buka level berikutnya (first sequence)
            bool thisLevelAllApproved = all.Where(x => x.Level == currentLevel)
                .All(x => x.Status == "Approved");

            if (thisLevelAllApproved)
            {
                var nextLevel = all.Where(x => x.Level > currentLevel)
                    .Select(x => x.Level)
                    .DefaultIfEmpty()
                    .Min();

                if (nextLevel != 0) // ada level berikutnya
                {
                    var firstSeqNextLevel = all.Where(x => x.Level == nextLevel)
                        .Select(x => x.SequenceOrder)
                        .Min();

                    // ? Promote SEMUA step pada first sequence di level berikutnya
                    foreach (
                        var step in all.Where(x =>
                            x.Level == nextLevel
                            && x.SequenceOrder == firstSeqNextLevel
                            && x.Status == "Blocked"
                        )
                    )
                        step.Status = "Pending";

                    await _context.SaveChangesAsync();
                    return (false, ProcurementId);
                }
            }

            // 5) Tidak ada next sequence/level ? cek apakah semua approval dok ini sudah Approved
            bool thisDocAllApproved = all.All(x => x.Status == "Approved");
            if (thisDocAllApproved)
            {
                var docForUpdate = await _context.ProcDocuments.FirstAsync(d =>
                    d.ProcDocumentId == docId
                );
                docForUpdate.IsApproved = true;
                docForUpdate.ApprovedAt = DateTime.UtcNow;
                docForUpdate.Status = "Approved";

                await _context.SaveChangesAsync();
            }

            // 6) Apakah SEMUA dokumen pada Procurement ini sudah approved?
            bool allDocsApproved = await _context
                .ProcDocumentApprovals.Include(a => a.ProcDocument)
                .Where(a => a.ProcDocument!.ProcurementId == ProcurementId)
                .AllAsync(a => a.Status == "Approved");

            return (allDocsApproved, ProcurementId);
        }

        public async Task RejectAsync(string approvalId, string approverUserId, string? note)
        {
            var approval =
                await _context
                    .ProcDocumentApprovals.Include(a => a.ProcDocument)
                    .ThenInclude(d => d.Procurement)
                    .FirstOrDefaultAsync(a => a.ProcDocumentApprovalId == approvalId)
                ?? throw new InvalidOperationException("Approval tidak ditemukan.");

            approval.Status = "Rejected";
            approval.ApproverId = approverUserId;
            approval.ApprovedAt = DateTime.UtcNow;
            approval.Note = note;

            var doc = await _context.ProcDocuments.FirstAsync(d =>
                d.ProcDocumentId == approval.ProcDocumentId
            );
            doc.Status = "Rejected";
            doc.IsApproved = false;

            await _context.SaveChangesAsync();
        }

        public async Task<GateInfoDto?> GetCurrentPendingGateByQrAsync(
            string qrText,
            CancellationToken ct = default
        )
        {
            // QrText diindeks di ProcDocuments, jadi query ini efisien
            var doc = await _context
                .ProcDocuments.Include(d => d.Approvals)!
                .ThenInclude(a => a.Role)
                .FirstOrDefaultAsync(d => d.QrText == qrText, ct);

            return BuildGate(doc);
        }

        public async Task<GateInfoDto?> GetCurrentPendingGateByApprovalIdAsync(
            string procDocumentApprovalId,
            CancellationToken ct = default
        )
        {
            var approval = await _context
                .ProcDocumentApprovals.Include(a => a.ProcDocument)!
                .ThenInclude(d => d.Approvals)!
                .ThenInclude(a => a.Role)
                .FirstOrDefaultAsync(a => a.ProcDocumentApprovalId == procDocumentApprovalId, ct);

            return BuildGate(approval?.ProcDocument);
        }

        private static GateInfoDto? BuildGate(ProcDocuments? doc)
        {
            if (doc == null)
                return null;

            var gate = new GateInfoDto
            {
                ProcurementId = doc.ProcurementId,
                ProcDocumentId = doc.ProcDocumentId,
                DocStatus = doc.Status,
            };

            var pendings = (doc.Approvals ?? []).Where(a => a.Status == "Pending").ToList();
            if (pendings.Count == 0)
                return gate;

            var minLevel = pendings.Min(a => a.Level);
            var minSeq = pendings.Where(a => a.Level == minLevel).Min(a => a.SequenceOrder);

            gate.Level = minLevel;
            gate.SequenceOrder = minSeq;

            gate.RequiredRoles =
            [
                .. doc.Approvals!.Where(a =>
                        a.Status == "Pending" && a.Level == minLevel && a.SequenceOrder == minSeq
                    )
                    .Select(a => new RoleInfoDto
                    { // <— dari Core.DTOs
                        RoleId = a.RoleId,
                        RoleName = a.Role?.Name,
                        ProcDocumentApprovalId = a.ProcDocumentApprovalId,
                    }),
            ];

            return gate;
        }

        public async Task AddStepIfMissingAsync(
            string procDocumentId,
            string procurementId,
            int level,
            int sequenceOrder,
            string roleId,
            CancellationToken ct = default
        )
        {
            // Cek dulu apakah step ini sudah ada (idempotent)
            var exists = await _context
                .ProcDocumentApprovals.AsNoTracking()
                .AnyAsync(
                    a =>
                        a.ProcDocumentId == procDocumentId
                        && a.Level == level
                        && a.SequenceOrder == sequenceOrder,
                    ct
                );

            if (exists)
                return;

            // Insert step baru (step pertama ? Pending, sisanya ? Blocked)
            var status = (level == 1 && sequenceOrder == 1) ? "Pending" : "Blocked";

            _context.ProcDocumentApprovals.Add(
                new ProcDocumentApprovals
                {
                    ProcDocumentId = procDocumentId,
                    ProcurementId = procurementId, // kalau kamu putuskan menghapus kolom ini nanti, cukup hilangkan baris ini
                    Level = level,
                    SequenceOrder = sequenceOrder,
                    RoleId = roleId,
                    Status = status,
                }
            );

            await _context.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<string>> GetUserRoleNamesAsync(
            string userId,
            CancellationToken ct = default
        )
        {
            var roleNames = await (
                from ur in _context.UserRoles.AsNoTracking()
                join r in _context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where ur.UserId == userId
                select r.Name!
            )
                .Distinct()
                .ToListAsync(ct);

            // Normalize: buang null/whitespace (jaga-jaga)
            return roleNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        }

        public async Task<IReadOnlyList<string>> GetUserRoleIdsAsync(
            string userId,
            CancellationToken ct = default
        )
        {
            var roleIds = await _context
                .UserRoles.AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .Distinct()
                .ToListAsync(ct);

            return roleIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
        }

        public async Task<ProcDocumentApprovals?> GetLastApprovalByUserOnDocumentAsync(
            string userId,
            string procDocumentId,
            CancellationToken ct = default
        )
        {
            return await _context
                .ProcDocumentApprovals.AsNoTracking()
                .Where(a =>
                    a.ProcDocumentId == procDocumentId
                    && a.ApproverId == userId
                    && a.Status == "Approved"
                )
                .OrderByDescending(a => a.ApprovedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<string>> GetExistingRoleIdsAsync(
            IEnumerable<string> roleIds,
            CancellationToken ct = default
        )
        {
            var ids = (roleIds ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return Array.Empty<string>();

            var exist = await _context
                .Roles.AsNoTracking()
                .Where(r => ids.Contains(r.Id))
                .Select(r => r.Id)
                .ToListAsync(ct);

            return exist;
        }

        public async Task<IReadOnlyList<string>> GetExistingRoleNamesAsync(
            IEnumerable<string> roleNames,
            CancellationToken ct = default
        )
        {
            var names = (roleNames ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (names.Count == 0)
                return Array.Empty<string>();

            var upper = names.Select(n => n.ToUpperInvariant()).ToList();

            var exist = await _context
                .Roles.AsNoTracking()
                .Where(r => r.Name != null && upper.Contains(r.Name.ToUpper()))
                .Select(r => r.Name!)
                .ToListAsync(ct);

            return exist;
        }

        public async Task<int> CountUsersWithAnyRoleAsync(
            IEnumerable<string> roleIds,
            IEnumerable<string> roleNames,
            CancellationToken ct = default
        )
        {
            var ids = (roleIds ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct()
                .ToList();

            var names = (roleNames ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Map names -> roleIds
            var roleIdsFromNames = await _context
                .Roles.AsNoTracking()
                .Where(r => r.Name != null && names.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync(ct);

            var targetRoleIds = ids.Union(roleIdsFromNames).Distinct().ToList();
            if (targetRoleIds.Count == 0)
                return 0;

            var count = await _context
                .UserRoles.AsNoTracking()
                .Where(ur => targetRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .CountAsync(ct);

            return count;
        }

        // Di ApprovalRepository
        public async Task<IReadOnlyList<ApprovalStepDto>> GetDocumentApprovalChainAsync(
            string procDocumentId,
            CancellationToken ct
        )
        {
            return await _context
                .ProcDocumentApprovals.Where(x => x.ProcDocumentId == procDocumentId)
                .Include(x => x.Role)
                .Include(x => x.Approver) // supaya full name bisa diambil
                .OrderBy(x => x.Level)
                .ThenBy(x => x.SequenceOrder)
                .Select(x => new ApprovalStepDto
                {
                    ProcDocumentApprovalId = x.ProcDocumentApprovalId,
                    Level = x.Level,
                    SequenceOrder = x.SequenceOrder,
                    RoleId = x.RoleId,
                    RoleName = x.Role.Name,
                    Status = x.Status, // <— PENTING
                    ApproverUserId = x.ApproverId,
                    ApproverFullName =
                        x.Approver != null ? (x.Approver.FullName ?? x.Approver.UserName) : null,
                    ApprovedAt = x.ApprovedAt,
                    Note = x.Note,
                })
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<GateInfoDto?> GetCurrentPendingGateByDocumentIdAsync(
            string procDocumentId,
            CancellationToken ct = default
        )
        {
            var doc = await _context
                .ProcDocuments.AsNoTracking()
                .Where(d => d.ProcDocumentId == procDocumentId)
                .Select(d => new
                {
                    d.ProcDocumentId,
                    d.ProcurementId,
                    d.Status,
                })
                .FirstOrDefaultAsync(ct);

            if (doc == null)
                return null;

            // Ambil approvals untuk dokumen ini
            var approvals = await _context
                .ProcDocumentApprovals.AsNoTracking()
                .Include(a => a.Role)
                .Where(a => a.ProcDocumentId == procDocumentId)
                .ToListAsync(ct);

            // Kalau tidak ada Pending sama sekali ? finalized
            var pendings = approvals.Where(a => a.Status == "Pending").ToList();
            if (pendings.Count == 0)
            {
                return new GateInfoDto
                {
                    ProcurementId = doc.ProcurementId,
                    ProcDocumentId = doc.ProcDocumentId,
                    DocStatus = doc.Status,
                    Level = null,
                    SequenceOrder = null,
                    RequiredRoles = new List<RoleInfoDto>(), // kosong ? AlreadyFinalized
                };
            }

            // Gate aktif = level terendah + sequence terendah yang PENDING
            var minLevel = pendings.Min(a => a.Level);
            var minSeq = pendings.Where(a => a.Level == minLevel).Min(a => a.SequenceOrder);

            var required = pendings
                .Where(a => a.Level == minLevel && a.SequenceOrder == minSeq)
                .Select(a => new RoleInfoDto
                {
                    RoleId = a.RoleId,
                    RoleName = a.Role != null ? a.Role.Name : null,
                    ProcDocumentApprovalId = a.ProcDocumentApprovalId,
                    Level = a.Level,
                    SequenceOrder = a.SequenceOrder,
                })
                .ToList();

            return new GateInfoDto
            {
                ProcurementId = doc.ProcurementId,
                ProcDocumentId = doc.ProcDocumentId,
                DocStatus = doc.Status,
                Level = minLevel,
                SequenceOrder = minSeq,
                RequiredRoles = required,
            };
        }

        public async Task<RejectionInfoDto?> GetLastRejectionInfoAsync(
            string procDocumentId,
            CancellationToken ct = default
        )
        {
            return await _context
                .ProcDocumentApprovals.AsNoTracking()
                .Include(a => a.Approver) // ambil nama user
                .Where(a => a.ProcDocumentId == procDocumentId && a.Status == "Rejected")
                .OrderByDescending(a => a.ApprovedAt) // waktu penolakan disimpan di ApprovedAt
                .Select(a => new RejectionInfoDto
                {
                    ProcDocumentApprovalId = a.ProcDocumentApprovalId,
                    RejectedByUserId = a.ApproverId,
                    RejectedByUserName = a.Approver != null ? a.Approver.UserName : null,
                    RejectedByFullName = a.Approver != null ? a.Approver.FullName : null,
                    RejectedAt = a.ApprovedAt, // lihat catatan di atas
                    RejectNote = a.Note,
                    Level = a.Level,
                    SequenceOrder = a.SequenceOrder,
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
