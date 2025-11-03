using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories {
    public class ApprovalRepository : IApprovalRepository {
        private readonly AppDbContext _context;
        private readonly ILogger<ApprovalRepository> _logger;

        public ApprovalRepository(AppDbContext context, ILogger<ApprovalRepository> logger) {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<WoDocumentApprovals>> GetPendingApprovalsForUserAsync(User user) {
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            return await _context.WoDocumentApprovals
                .Include(a => a.WoDocument).ThenInclude(d => d.DocumentType)
                .Include(a => a.WorkOrder)
                .Include(a => a.Role)
                .Where(a => a.Status == "Pending" && roles.Contains(a.RoleId))
                .AsNoTracking()
                .ToListAsync();
        }

        //public async Task<(bool AllDocsApproved, string WorkOrderId)> ApproveAsync(string approvalId, string approverUserId) {
        //    var approval = await _context.WoDocumentApprovals
        //        .Include(a => a.WoDocument).ThenInclude(doc => doc.WorkOrder)
        //        .FirstOrDefaultAsync(a => a.WoDocumentApprovalId == approvalId)
        //        ?? throw new InvalidOperationException("Approval tidak ditemukan.");

        //    var wo = approval.WoDocument.WorkOrder
        //        ?? throw new InvalidOperationException("Work Order tidak ditemukan.");
        //    var woId = wo.WorkOrderId;

        //    // Ambil total penawaran untuk menentukan apakah VP dibutuhkan
        //    const decimal ThresholdVP = 500_000_000m;
        //    var pnl = await _context.ProfitLosses
        //        .AsNoTracking()
        //        .Where(p => p.WorkOrderId == woId)
        //        .OrderByDescending(p => p.CreatedAt)
        //        .FirstOrDefaultAsync();

        //    var totalOffer = pnl?.SelectedVendorFinalOffer ?? 0m;
        //    var needVP = totalOffer > ThresholdVP;

        //    _logger.LogInformation("[Approve] WO={WO}, TotalOffer={TotalOffer:N0}, NeedVP={NeedVP}",
        //        wo.WoNum, totalOffer, needVP);

        //    // Ambil konfigurasi tahapan approval
        //    var config = await _context.WoTypesDocuments
        //        .Include(x => x.DocumentApprovals).ThenInclude(a => a.Role)
        //        .FirstOrDefaultAsync(x =>
        //            x.WoTypeId == wo.WoTypeId &&
        //            x.DocumentTypeId == approval.WoDocument.DocumentTypeId);

        //    if (config == null)
        //        throw new InvalidOperationException("Konfigurasi approval tidak ditemukan.");

        //    // Susun chain berdasarkan urutan, lalu filter VP kalau tidak perlu
        //    var chain = config.DocumentApprovals
        //        .Where(a => a.Role != null)
        //        .OrderBy(a => a.SequenceOrder)
        //        .ToList();

        //    if (!needVP) {
        //        chain = chain
        //            .Where(a => !string.Equals(a.Role!.Name, "Vice President", StringComparison.OrdinalIgnoreCase))
        //            .OrderBy(a => a.SequenceOrder)
        //            .ToList();
        //    }

        //    // Pastikan chain ada
        //    if (chain.Count == 0)
        //        throw new InvalidOperationException("Chain approval kosong setelah filter.");

        //    // Cari index current step di chain
        //    var currentIndex = chain.FindIndex(c =>
        //        c.RoleId == approval.RoleId &&
        //        c.SequenceOrder == approval.SequenceOrder);

        //    if (currentIndex < 0)
        //        currentIndex = 0; // fallback ke awal kalau tidak cocok

        //    var isLast = currentIndex >= chain.Count - 1;

        //    if (!isLast) {
        //        var next = chain[currentIndex + 1];

        //        // Geser step ke role selanjutnya
        //        approval.RoleId = next.RoleId;
        //        approval.Level = next.Level;
        //        approval.SequenceOrder = next.SequenceOrder;
        //        approval.Status = "Pending";
        //        approval.ApproverId = null;
        //        approval.ApprovedAt = null;
        //        approval.Note = null;

        //        await _context.SaveChangesAsync();
        //        _logger.LogInformation("[Approve] WO={WO}, Doc={Doc}, Move to next step {Seq} ({Role})",
        //            wo.WoNum, approval.WoDocumentId, next.SequenceOrder, next.Role?.Name);

        //        return (false, woId);
        //    }

        //    // Kalau sudah tahap terakhir
        //    approval.Status = "Approved";
        //    approval.ApproverId = approverUserId;
        //    approval.ApprovedAt = DateTime.Now;

        //    // Update dokumen
        //    var doc = approval.WoDocument;
        //    doc.Status = "Approved";
        //    doc.IsApproved = true;
        //    doc.ApprovedAt = DateTime.Now;

        //    await _context.SaveChangesAsync();

        //    // Cek apakah semua dokumen untuk WO sudah diapprove
        //    bool allDocsApproved = await _context.WoDocumentApprovals
        //        .Include(a => a.WoDocument)
        //        .Where(a => a.WoDocument.WorkOrderId == woId)
        //        .AllAsync(a => a.Status == "Approved");

        //    return (allDocsApproved, woId);
        //}

        //public async Task ApproveAsync(string approvalId, string approverUserId) {
        //    var approval = await _context.WoDocumentApprovals
        //        .Include(a => a.WoDocument)
        //        .FirstOrDefaultAsync(a => a.WoDocumentApprovalId == approvalId)
        //        ?? throw new InvalidOperationException("Approval tidak ditemukan.");

        //    if (approval.Status is "Approved" or "Rejected")
        //        return; // idempotent

        //    // 1) Set current approved
        //    approval.Status = "Approved";
        //    approval.ApproverId = approverUserId;
        //    approval.ApprovedAt = DateTime.Now;

        //    // 2) Ambil semua step utk dokumen ini (track perubahan)
        //    var all = await _context.WoDocumentApprovals
        //        .Where(a => a.WoDocumentId == approval.WoDocumentId)
        //        .ToListAsync();

        //    // Kalau ada yang Rejected sebelumnya, berhenti (dok sudah mental)
        //    if (all.Any(x => x.Status == "Rejected")) {
        //        await _context.SaveChangesAsync();
        //        return;
        //    }

        //    var docId = approval.WoDocumentId;
        //    var currentLevel = approval.Level;
        //    var currentSeq = approval.SequenceOrder;

        //    // 3) Cek apakah masih ada step berikutnya di LEVEL yang sama
        //    var nextInSameLevel = all
        //        .Where(x => x.Level == currentLevel && x.SequenceOrder > currentSeq)
        //        .OrderBy(x => x.SequenceOrder)
        //        .FirstOrDefault(x => x.Status == "Blocked");

        //    if (nextInSameLevel != null) {
        //        // Promote next sequence dalam level yang sama
        //        nextInSameLevel.Status = "Pending";
        //        await _context.SaveChangesAsync();
        //        return;
        //    }

        //    // 4) Kalau semua step pada level saat ini sudah approved, naik ke LEVEL berikutnya
        //    bool thisLevelAllApproved = all
        //        .Where(x => x.Level == currentLevel)
        //        .All(x => x.Status == "Approved");

        //    if (thisLevelAllApproved) {
        //        var nextLevel = all
        //            .Where(x => x.Level > currentLevel)
        //            .Select(x => x.Level)
        //            .DefaultIfEmpty()
        //            .Min();

        //        if (nextLevel != 0) // ada level berikutnya
        //        {
        //            // Set step pertama (seq paling kecil) di level berikutnya menjadi Pending
        //            var firstSeqNextLevel = all
        //                .Where(x => x.Level == nextLevel)
        //                .Select(x => x.SequenceOrder)
        //                .Min();

        //            var firstStepNextLevel = all
        //                .First(x => x.Level == nextLevel && x.SequenceOrder == firstSeqNextLevel);

        //            if (firstStepNextLevel.Status == "Blocked")
        //                firstStepNextLevel.Status = "Pending";

        //            await _context.SaveChangesAsync();
        //            return;
        //        }
        //    }

        //    // 5) Kalau sampai sini: tidak ada next sequence/level -> semua level selesai?
        //    bool allApproved = all.All(x => x.Status == "Approved");
        //    if (allApproved) {
        //        var doc = await _context.WoDocuments.FirstAsync(d => d.WoDocumentId == docId);
        //        doc.IsApproved = true;
        //        doc.ApprovedAt = DateTime.Now;
        //        doc.Status = "Approved";

        //        await _context.SaveChangesAsync();
        //    }
        //}

        public async Task<(bool AllDocsApproved, string WorkOrderId)> ApproveAsync(string approvalId, string approverUserId) {
            var approval = await _context.WoDocumentApprovals
                .Include(a => a.WoDocument).ThenInclude(doc => doc.WorkOrder)
                .FirstOrDefaultAsync(a => a.WoDocumentApprovalId == approvalId)
                ?? throw new InvalidOperationException("Approval tidak ditemukan.");

            var wo = approval.WoDocument.WorkOrder
            ?? throw new InvalidOperationException("Work Order tidak ditemukan.");
            var woId = wo.WorkOrderId;

            if (approval.Status is "Approved" or "Rejected")
                return (false, woId); // idempotent

            // Ambil total penawaran untuk menentukan apakah VP dibutuhkan
            const decimal ThresholdVP = 500_000_000m;
            var pnl = await _context.ProfitLosses
            .AsNoTracking()
            .Where(p => p.WorkOrderId == woId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

            var totalOffer = pnl?.SelectedVendorFinalOffer ?? 0m;
            var needVP = totalOffer > ThresholdVP;

            _logger.LogInformation("[Approve] WO={WO}, TotalOffer={TotalOffer:N0}, NeedVP={NeedVP}",
            wo.WoNum, totalOffer, needVP);

            // 1) Set current approved
            approval.Status = "Approved";
            approval.ApproverId = approverUserId;
            approval.ApprovedAt = DateTime.Now;

            // 2) Ambil semua step utk dokumen ini (track perubahan)
            var all = await _context.WoDocumentApprovals
                .Where(a => a.WoDocumentId == approval.WoDocumentId)
                .ToListAsync();

            // Kalau ada yang Rejected sebelumnya, berhenti (dok sudah mental)
            if (all.Any(x => x.Status == "Rejected")) {
                await _context.SaveChangesAsync();
                return (false, woId);
            }

            var docId = approval.WoDocumentId;
            var currentLevel = approval.Level;
            var currentSeq = approval.SequenceOrder;

            // 3) Cek apakah masih ada step berikutnya di LEVEL yang sama
            var nextInSameLevel = all
                .Where(x => x.Level == currentLevel && x.SequenceOrder > currentSeq)
                .OrderBy(x => x.SequenceOrder)
                .FirstOrDefault(x => x.Status == "Blocked");

            if (nextInSameLevel != null) {
                // Promote next sequence dalam level yang sama
                nextInSameLevel.Status = "Pending";
                await _context.SaveChangesAsync();
                return (false, woId);
            }

            // 4) Kalau semua step pada level saat ini sudah approved, naik ke LEVEL berikutnya
            bool thisLevelAllApproved = all
                .Where(x => x.Level == currentLevel)
                .All(x => x.Status == "Approved");

            if (thisLevelAllApproved) {
                var nextLevel = all
                    .Where(x => x.Level > currentLevel)
                    .Select(x => x.Level)
                    .DefaultIfEmpty()
                    .Min();

                if (nextLevel != 0) // ada level berikutnya
                {
                    // Set step pertama (seq paling kecil) di level berikutnya menjadi Pending
                    var firstSeqNextLevel = all
                        .Where(x => x.Level == nextLevel)
                        .Select(x => x.SequenceOrder)
                        .Min();

                    var firstStepNextLevel = all
                        .First(x => x.Level == nextLevel && x.SequenceOrder == firstSeqNextLevel);

                    if (firstStepNextLevel.Status == "Blocked")
                        firstStepNextLevel.Status = "Pending";

                    await _context.SaveChangesAsync();
                    return (false, woId);
                }
            }

            // 5) Kalau sampai sini: tidak ada next sequence/level -> semua level selesai?
            bool allApproved = all.All(x => x.Status == "Approved");
            if (allApproved) {
                var doc = await _context.WoDocuments.FirstAsync(d => d.WoDocumentId == docId);
                doc.IsApproved = true;
                doc.ApprovedAt = DateTime.Now;
                doc.Status = "Approved";

                await _context.SaveChangesAsync();
            }

            // Cek apakah semua dokumen untuk WO sudah diapprove
            bool allDocsApproved = await _context.WoDocumentApprovals
            .Include(a => a.WoDocument)
            .Where(a => a.WoDocument.WorkOrderId == woId)
            .AllAsync(a => a.Status == "Approved");

            return (allDocsApproved, woId);
        }


        public async Task RejectAsync(string approvalId, string approverUserId, string? note) {
            var approval = await _context.WoDocumentApprovals
                .Include(a => a.WoDocument).ThenInclude(d => d.WorkOrder)
                .FirstOrDefaultAsync(a => a.WoDocumentApprovalId == approvalId)
                ?? throw new InvalidOperationException("Approval tidak ditemukan.");

            approval.Status = "Rejected";
            approval.ApproverId = approverUserId;
            approval.ApprovedAt = DateTime.UtcNow;
            approval.Note = note;

            var doc = await _context.WoDocuments.FirstAsync(d => d.WoDocumentId == approval.WoDocumentId);
            doc.Status = "Rejected";
            doc.IsApproved = false;

            await _context.SaveChangesAsync();
        }
    }
}
