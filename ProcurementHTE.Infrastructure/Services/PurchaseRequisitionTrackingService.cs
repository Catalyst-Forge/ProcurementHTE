using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;
using System.ComponentModel;
using System.Reflection;

namespace ProcurementHTE.Infrastructure.Services
{
    public class PurchaseRequisitionTrackingService : IPurchaseRequisitionTrackingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PurchaseRequisitionTrackingService> _logger;

        public PurchaseRequisitionTrackingService(
            AppDbContext context,
            ILogger<PurchaseRequisitionTrackingService> logger
        )
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PRTrackingDto?> GetTrackingByPrNumberAsync(
            string prNumber,
            CancellationToken ct = default
        )
        {
            var pr = await _context
                .PurchaseRequisitions.AsNoTracking()
                .Include(p => p.CreatedByUser)
                .Include(p => p.IspaSubmittedByUser)
                .Include(p => p.PoSubmittedByUser)
                .Include(p => p.RejectedByUser)
                .Include(p => p.StatusHistories)
                    .ThenInclude(h => h.ChangedByUser)
                .Include(p => p.Procurements)
                    .ThenInclude(proc => proc.ProcDocuments)
                .Include(p => p.Procurements)
                    .ThenInclude(proc => proc.JobType)
                        .ThenInclude(jt => jt!.JobTypeDocuments)
                            .ThenInclude(jtd => jtd.DocumentType)
                .FirstOrDefaultAsync(p => p.PrNumber == prNumber, ct);

            if (pr == null) return null;
            
            // Get procurement IDs for ProfitLoss lookup
            var procurementIds = pr.Procurements?.Select(p => p.ProcurementId).ToList() ?? [];
            var needsJustifikasiMap = await GetNeedsJustifikasiMapAsync(procurementIds, ct);
            
            return MapToDto(pr, needsJustifikasiMap);
        }

        public async Task<PRTrackingDto?> GetTrackingByPrIdAsync(
            string prId,
            CancellationToken ct = default
        )
        {
            var pr = await _context
                .PurchaseRequisitions.AsNoTracking()
                .Include(p => p.CreatedByUser)
                .Include(p => p.IspaSubmittedByUser)
                .Include(p => p.PoSubmittedByUser)
                .Include(p => p.RejectedByUser)
                .Include(p => p.StatusHistories)
                    .ThenInclude(h => h.ChangedByUser)
                .Include(p => p.Procurements)
                    .ThenInclude(proc => proc.ProcDocuments)
                .Include(p => p.Procurements)
                    .ThenInclude(proc => proc.JobType)
                        .ThenInclude(jt => jt!.JobTypeDocuments)
                            .ThenInclude(jtd => jtd.DocumentType)
                .FirstOrDefaultAsync(p => p.PrId == prId, ct);
            
            if (pr == null) return null;
            
            // Get procurement IDs for ProfitLoss lookup
            var procurementIds = pr.Procurements?.Select(p => p.ProcurementId).ToList() ?? [];
            var needsJustifikasiMap = await GetNeedsJustifikasiMapAsync(procurementIds, ct);

            return MapToDto(pr, needsJustifikasiMap);
        }

        public async Task<bool> UpdatePrStatusAsync(
            string prId,
            PurchaseRequisitionStatus newStatus,
            string? changedByUserId = null,
            string? note = null,
            CancellationToken ct = default
        )
        {
            var pr = await _context.PurchaseRequisitions.FindAsync([prId], ct);
            if (pr == null)
                return false;

            var oldStatus = pr.Status;
            pr.Status = newStatus;
            pr.UpdatedAt = DateTime.UtcNow;

            // Log to history
            _context.PurchaseRequisitionStatusHistories.Add(
                new PurchaseRequisitionStatusHistory
                {
                    PrId = prId,
                    Status = newStatus,
                    ChangedAt = DateTime.UtcNow,
                    ChangedByUserId = changedByUserId,
                    Note = note,
                }
            );

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "PR {PrId} status changed from {OldStatus} to {NewStatus} by user {UserId}",
                prId,
                oldStatus,
                newStatus,
                changedByUserId ?? "System"
            );

            return true;
        }

        public async Task<PRTrackingResponse> SubmitIspaAsync(
            string prId,
            string ispaNumber,
            string submittedByUserId,
            CancellationToken ct = default
        )
        {
            var pr = await _context.PurchaseRequisitions.FindAsync([prId], ct);
            if (pr == null)
                return new PRTrackingResponse
                {
                    Success = false,
                    Message = "Purchase Requisition tidak ditemukan.",
                };

            if (pr.Status != PurchaseRequisitionStatus.OnSubmitISPA)
                return new PRTrackingResponse
                {
                    Success = false,
                    Message = $"Status PR saat ini adalah {GetStatusDescription(pr.Status)}. ISPA hanya bisa disubmit saat status 'On Submit ISPA'.",
                };

            pr.IspaNumber = ispaNumber;
            pr.IspaSubmittedAt = DateTime.UtcNow;
            pr.IspaSubmittedByUserId = submittedByUserId;
            pr.UpdatedAt = DateTime.UtcNow;

            await UpdatePrStatusAsync(
                prId,
                PurchaseRequisitionStatus.OnSubmitHardcopy,
                submittedByUserId,
                $"ISPA Number: {ispaNumber}",
                ct
            );

            var tracking = await GetTrackingByPrIdAsync(prId, ct);

            return new PRTrackingResponse
            {
                Success = true,
                Message = "ISPA berhasil disubmit.",
                Data = tracking,
            };
        }

        public async Task<PRTrackingResponse> SubmitJustificationAsync(
            string prId,
            string submittedByUserId,
            CancellationToken ct = default
        )
        {
            var pr = await _context.PurchaseRequisitions.FindAsync([prId], ct);
            if (pr == null)
                return new PRTrackingResponse
                {
                    Success = false,
                    Message = "Purchase Requisition tidak ditemukan.",
                };

            if (pr.Status != PurchaseRequisitionStatus.OnSubmitHardcopy)
                return new PRTrackingResponse
                {
                    Success = false,
                    Message = $"Status PR saat ini adalah {GetStatusDescription(pr.Status)}. Justifikasi hanya bisa disubmit saat status 'On Submit Hardcopy'.",
                };

            pr.UpdatedAt = DateTime.UtcNow;

            await UpdatePrStatusAsync(
                prId,
                PurchaseRequisitionStatus.OnSubmitPO,
                submittedByUserId,
                "Justifikasi submitted",
                ct
            );

            var tracking = await GetTrackingByPrIdAsync(prId, ct);

            return new PRTrackingResponse
            {
                Success = true,
                Message = "Justifikasi berhasil disubmit.",
                Data = tracking,
            };
        }

        public async Task<PRTrackingResponse> SubmitPoAsync(
            string prId,
            string poNumber,
            string submittedByUserId,
            CancellationToken ct = default
        )
        {
            var pr = await _context.PurchaseRequisitions.FindAsync([prId], ct);
            if (pr == null)
                return new PRTrackingResponse
                {
                    Success = false,
                    Message = "Purchase Requisition tidak ditemukan.",
                };

            if (pr.Status != PurchaseRequisitionStatus.OnSubmitPO)
                return new PRTrackingResponse
                {
                    Success = false,
                    Message = $"Status PR saat ini adalah {GetStatusDescription(pr.Status)}. PO hanya bisa disubmit saat status 'On Submit PO'.",
                };

            pr.PoNumber = poNumber;
            pr.PoSubmittedAt = DateTime.UtcNow;
            pr.PoSubmittedByUserId = submittedByUserId;
            pr.UpdatedAt = DateTime.UtcNow;

            await UpdatePrStatusAsync(
                prId,
                PurchaseRequisitionStatus.DonePO,
                submittedByUserId,
                $"PO Number: {poNumber}",
                ct
            );

            var tracking = await GetTrackingByPrIdAsync(prId, ct);

            return new PRTrackingResponse
            {
                Success = true,
                Message = "PO berhasil disubmit. PR selesai!",
                Data = tracking,
            };
        }

        public async Task<PRTrackingResponse> SendForApprovalAsync(
            string prId,
            string sentByUserId,
            CancellationToken ct = default
        )
        {
            var pr = await _context.PurchaseRequisitions.FindAsync([prId], ct);
            if (pr == null)
                return new PRTrackingResponse
                {
                    Success = false,
                    Message = "Purchase Requisition tidak ditemukan.",
                };

            if (pr.Status != PurchaseRequisitionStatus.OnCreateDP3)
                return new PRTrackingResponse
                {
                    Success = false,
                    Message = $"Status PR saat ini adalah {GetStatusDescription(pr.Status)}. Hanya PR dengan status 'On Create DP3' yang bisa dikirim untuk approval.",
                };

            // Generate unique approval token
            var token = GenerateApprovalToken();
            
            pr.ApprovalToken = token;
            pr.ApprovalTokenGeneratedAt = DateTime.UtcNow;
            pr.ApprovalSentByUserId = sentByUserId;
            pr.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await UpdatePrStatusAsync(
                prId,
                PurchaseRequisitionStatus.WaitingApprovalAnalyst,
                sentByUserId,
                "Send for approval - QR Code generated",
                ct
            );

            _logger.LogInformation(
                "PR {PrId} sent for approval by {UserId}. Token: {Token}",
                prId,
                sentByUserId,
                token
            );

            var tracking = await GetTrackingByPrIdAsync(prId, ct);

            return new PRTrackingResponse
            {
                Success = true,
                Message = "PR berhasil dikirim untuk approval. QR Code telah di-generate.",
                Data = tracking,
            };
        }

        /// <summary>
        /// Generate unique approval token untuk QR Code
        /// Format: APPR-{shortGuid}-{timestamp}
        /// </summary>
        private static string GenerateApprovalToken()
        {
            var shortGuid = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var timestamp = DateTime.UtcNow.ToString("yyMMddHHmm");
            return $"APPR-{shortGuid}-{timestamp}";
        }

        public async Task<PRTrackingResponse> RejectPrAsync(
            string prId,
            string rejectionNote,
            string rejectedByUserId,
            CancellationToken ct = default
        )
        {
            var pr = await _context.PurchaseRequisitions.FindAsync([prId], ct);
            if (pr == null)
                return new PRTrackingResponse
                {
                    Success = false,
                    Message = "Purchase Requisition tidak ditemukan.",
                };

            if (pr.Status == PurchaseRequisitionStatus.Rejected)
                return new PRTrackingResponse
                {
                    Success = false,
                    Message = "PR sudah dalam status Rejected.",
                };

            if (pr.Status == PurchaseRequisitionStatus.DonePO)
                return new PRTrackingResponse
                {
                    Success = false,
                    Message = "PR sudah selesai (Done PO), tidak bisa direject.",
                };

            pr.RejectionNote = rejectionNote;
            pr.RejectedAt = DateTime.UtcNow;
            pr.RejectedByUserId = rejectedByUserId;
            pr.UpdatedAt = DateTime.UtcNow;

            await UpdatePrStatusAsync(
                prId,
                PurchaseRequisitionStatus.Rejected,
                rejectedByUserId,
                rejectionNote,
                ct
            );

            var tracking = await GetTrackingByPrIdAsync(prId, ct);

            return new PRTrackingResponse
            {
                Success = true,
                Message = "PR berhasil direject.",
                Data = tracking,
            };
        }

        public async Task<bool> HandleApprovalStatusChangeAsync(
            string prId,
            string approvalAction,
            string? approverUserId = null,
            string? note = null,
            CancellationToken ct = default
        )
        {
            var pr = await _context.PurchaseRequisitions.FindAsync([prId], ct);
            if (pr == null)
            {
                _logger.LogWarning("PR {PrId} not found for approval status change", prId);
                return false;
            }

            var currentStatus = pr.Status;

            // Handle reject
            if (approvalAction.Equals("reject", StringComparison.OrdinalIgnoreCase))
            {
                await RejectPrAsync(prId, note ?? "Rejected by approver", approverUserId ?? "System", ct);
                return true;
            }

            // Handle approve - transition to next approval status
            var newStatus = currentStatus switch
            {
                PurchaseRequisitionStatus.WaitingApprovalAnalyst => PurchaseRequisitionStatus
                    .WaitingApprovalAsstManager,
                PurchaseRequisitionStatus.WaitingApprovalAsstManager => PurchaseRequisitionStatus
                    .WaitingApprovalManager,
                PurchaseRequisitionStatus.WaitingApprovalManager => PurchaseRequisitionStatus.OnSubmitISPA,
                _ => currentStatus, // No change for other statuses
            };

            if (newStatus != currentStatus)
            {
                await UpdatePrStatusAsync(prId, newStatus, approverUserId, note, ct);
                return true;
            }

            return false;
        }

        // Helper methods
        
        /// <summary>
        /// Get map of procurement ID to whether it needs Justifikasi document (value > 300 million)
        /// </summary>
        private async Task<Dictionary<string, bool>> GetNeedsJustifikasiMapAsync(
            List<string> procurementIds, 
            CancellationToken ct)
        {
            var result = new Dictionary<string, bool>();
            
            if (procurementIds.Count == 0)
                return result;

            // Get latest ProfitLoss for each procurement and check if value > 300 million
            var profitLossData = await _context.ProfitLosses
                .AsNoTracking()
                .Where(p => procurementIds.Contains(p.ProcurementId))
                .GroupBy(p => p.ProcurementId)
                .Select(g => new 
                { 
                    ProcurementId = g.Key, 
                    LatestPnl = g.OrderByDescending(p => p.CreatedAt).FirstOrDefault() 
                })
                .ToListAsync(ct);

            foreach (var procId in procurementIds)
            {
                var pnlData = profitLossData.FirstOrDefault(p => p.ProcurementId == procId);
                var needsJustifikasi = false;
                
                if (pnlData?.LatestPnl != null)
                {
                    var bestFinalOffer = pnlData.LatestPnl.SelectedVendorFinalOffer;
                    
                    if (bestFinalOffer <= 0m)
                    {
                        // Calculate from vendor offers
                        var offers = await _context.VendorOffers
                            .AsNoTracking()
                            .Where(o => o.ProfitLossId == pnlData.LatestPnl.ProfitLossId)
                            .ToListAsync(ct);

                        if (offers.Count > 0)
                        {
                            bestFinalOffer = offers
                                .GroupBy(o => o.VendorId)
                                .Select(group =>
                                {
                                    var perItem = group
                                        .GroupBy(x => x.ProcOfferId)
                                        .Select(gg =>
                                        {
                                            var last = gg.OrderBy(x => x.Round).Last();
                                            return last.Price * last.QuantityItem * last.QuantityOfUnit;
                                        });
                                    return perItem.Sum();
                                })
                                .DefaultIfEmpty(0m)
                                .Min();
                        }
                    }
                    
                    needsJustifikasi = bestFinalOffer > 300_000_000m;
                }
                
                result[procId] = needsJustifikasi;
            }
            
            return result;
        }
        
        private static PRTrackingDto MapToDto(PurchaseRequisition pr, Dictionary<string, bool> needsJustifikasiMap)
        {
            // Calculate mandatory documents status from linked procurements
            var linkedProcurementsCount = pr.Procurements?.Count ?? 0;
            var totalMandatoryDocs = 0;
            var uploadedMandatoryDocs = 0;

            if (pr.Procurements != null)
            {
                foreach (var procurement in pr.Procurements)
                {
                    // Check if this procurement needs Justifikasi document
                    var needsJustifikasi = needsJustifikasiMap.GetValueOrDefault(procurement.ProcurementId, false);
                    
                    // Get mandatory JobTypeDocuments for this procurement's job type
                    // Filter by ProcurementCategory (Jasa/Barang) - same logic as ProcurementDocumentQuery
                    // Also filter out Justifikasi if not needed (value <= 300 million)
                    var mandatoryJobTypeDocs = procurement.JobType?.JobTypeDocuments?
                        .Where(jtd => jtd.IsMandatory)
                        .Where(jtd => jtd.ProcurementCategory == null || jtd.ProcurementCategory == procurement.ProcurementCategory)
                        .Where(jtd => jtd.DocumentType?.Name != "Justifikasi" || needsJustifikasi)
                        .ToList() ?? [];

                    totalMandatoryDocs += mandatoryJobTypeDocs.Count;

                    // Check which mandatory docs are uploaded/generated
                    var procDocs = procurement.ProcDocuments ?? [];
                    foreach (var jtd in mandatoryJobTypeDocs)
                    {
                        // Match by DocumentTypeId since ProcDocuments doesn't have JobTypeDocumentId
                        var uploaded = procDocs.Any(pd =>
                            pd.DocumentTypeId == jtd.DocumentTypeId &&
                            !string.IsNullOrEmpty(pd.FileName));

                        if (uploaded)
                            uploadedMandatoryDocs++;
                    }
                }
            }

            // Calculate documents - status sekarang di level PR bukan per document
            // Semua document dianggap ada jika sudah di-upload
            var totalDocuments = 0;
            if (pr.Procurements != null)
            {
                foreach (var proc in pr.Procurements)
                {
                    if (proc.ProcDocuments != null)
                    {
                        totalDocuments += proc.ProcDocuments.Count;
                    }
                }
            }

            // Determine next approver role
            var nextApproverRole = pr.Status switch
            {
                PurchaseRequisitionStatus.WaitingApprovalAnalyst => "Analyst HTE",
                PurchaseRequisitionStatus.WaitingApprovalAsstManager => "Asst. Manager",
                PurchaseRequisitionStatus.WaitingApprovalManager => "Manager",
                _ => null
            };

            return new PRTrackingDto
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                RequestDate = pr.RequestDate,
                Description = pr.Description,
                CurrentStatus = pr.Status,
                CurrentStatusDescription = GetStatusDescription(pr.Status),
                IspaNumber = pr.IspaNumber,
                IspaSubmittedAt = pr.IspaSubmittedAt,
                IspaSubmittedByUserName = pr.IspaSubmittedByUser?.UserName,
                PoNumber = pr.PoNumber,
                PoSubmittedAt = pr.PoSubmittedAt,
                PoSubmittedByUserName = pr.PoSubmittedByUser?.UserName,
                RejectionNote = pr.RejectionNote,
                RejectedAt = pr.RejectedAt,
                RejectedByUserName = pr.RejectedByUser?.FullName,
                CreatedByUserName = pr.CreatedByUser?.UserName,
                CreatedByFullName = pr.CreatedByUser?.FullName,
                LinkedProcurementsCount = linkedProcurementsCount,
                TotalMandatoryDocs = totalMandatoryDocs,
                UploadedMandatoryDocs = uploadedMandatoryDocs,
                TotalDocuments = totalDocuments,
                NextApproverRole = nextApproverRole,
                // Generate QR URL using stored token
                ApprovalQrUrl = !string.IsNullOrEmpty(pr.ApprovalToken) 
                    ? $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString($"procurehte://approve/{pr.ApprovalToken}")}"
                    : null,
                StatusHistory = pr
                    .StatusHistories.OrderBy(h => h.ChangedAt)
                    .Select(h => new PRStatusHistoryDto
                    {
                        Id = h.Id,
                        Status = h.Status,
                        StatusDescription = GetStatusDescription(h.Status),
                        ChangedAt = h.ChangedAt,
                        ChangedByUserName = h.ChangedByUser?.UserName,
                        ChangedByFullName = h.ChangedByUser?.FullName,
                        Note = h.Note,
                    })
                    .ToList(),
            };
        }

        private static string GetStatusDescription(PurchaseRequisitionStatus status)
        {
            var field = status.GetType().GetField(status.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? status.ToString();
        }
    }
}
