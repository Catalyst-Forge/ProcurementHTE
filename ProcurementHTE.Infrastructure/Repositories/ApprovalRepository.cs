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

        public async Task<(bool AllDocsApproved, string WorkOrderId)> ApproveAsync(string approvalId, string approverUserId) {
            var approval = await _context.WoDocumentApprovals
                .Include(a => a.WoDocument).ThenInclude(doc => doc.WorkOrder)
                .FirstOrDefaultAsync(a => a.WoDocumentApprovalId == approvalId)
                ?? throw new InvalidOperationException("Approval tidak ditemukan.");

            var wo = approval.WoDocument.WorkOrder
                ?? throw new InvalidOperationException("Work Order tidak ditemukan.");
            var woId = wo.WorkOrderId;

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

            // Ambil konfigurasi tahapan approval
            var config = await _context.WoTypesDocuments
                .Include(x => x.DocumentApprovals).ThenInclude(a => a.Role)
                .FirstOrDefaultAsync(x =>
                    x.WoTypeId == wo.WoTypeId &&
                    x.DocumentTypeId == approval.WoDocument.DocumentTypeId);

            if (config == null)
                throw new InvalidOperationException("Konfigurasi approval tidak ditemukan.");

            // Susun chain berdasarkan urutan, lalu filter VP kalau tidak perlu
            var chain = config.DocumentApprovals
                .Where(a => a.Role != null)
                .OrderBy(a => a.SequenceOrder)
                .ToList();

            if (!needVP) {
                chain = chain
                    .Where(a => !string.Equals(a.Role!.Name, "Vice President", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(a => a.SequenceOrder)
                    .ToList();
            }

            // Pastikan chain ada
            if (chain.Count == 0)
                throw new InvalidOperationException("Chain approval kosong setelah filter.");

            // Cari index current step di chain
            var currentIndex = chain.FindIndex(c =>
                c.RoleId == approval.RoleId &&
                c.SequenceOrder == approval.SequenceOrder);

            if (currentIndex < 0)
                currentIndex = 0; // fallback ke awal kalau tidak cocok

            var isLast = currentIndex >= chain.Count - 1;

            if (!isLast) {
                var next = chain[currentIndex + 1];

                // Geser step ke role selanjutnya
                approval.RoleId = next.RoleId;
                approval.Level = next.Level;
                approval.SequenceOrder = next.SequenceOrder;
                approval.Status = "Pending";
                approval.ApproverId = null;
                approval.ApprovedAt = null;
                approval.Note = null;

                await _context.SaveChangesAsync();
                _logger.LogInformation("[Approve] WO={WO}, Doc={Doc}, Move to next step {Seq} ({Role})",
                    wo.WoNum, approval.WoDocumentId, next.SequenceOrder, next.Role?.Name);

                return (false, woId);
            }

            // Kalau sudah tahap terakhir
            approval.Status = "Approved";
            approval.ApproverId = approverUserId;
            approval.ApprovedAt = DateTime.Now;

            // Update dokumen
            var doc = approval.WoDocument;
            doc.Status = "Approved";
            doc.IsApproved = true;
            doc.ApprovedAt = DateTime.Now;

            await _context.SaveChangesAsync();

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
