using System.Collections.Generic;
using System.Linq;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class LdpRepository
    {
        private static string? BuildKeterangan(Core.Models.Procurement p)
        {
            if (p.ProcurementStatus != ProcurementStatus.Cancelled)
                return p.Note;

            return string.IsNullOrWhiteSpace(p.Note) ? "Cancel" : $"Cancel - {p.Note}";
        }

        private static LdpRecapDto MapToDto(Core.Models.Procurement p, List<Core.Models.VendorRoundLetter> vendorRoundLetters)
        {
            var profitLoss = p.ProfitLosses.FirstOrDefault();
            var selectedVendor = profitLoss?.SelectedVendor;
            var pr = p.PurchaseRequisition;

            var procDocs = p.ProcDocuments ?? new List<Core.Models.ProcDocuments>();

            // Get vendor round letter for selected vendor
            var vendorLetter =
                selectedVendor != null
                    ? vendorRoundLetters.FirstOrDefault(vrl =>
                        vrl.ProcurementId == p.ProcurementId
                        && vrl.VendorId == selectedVendor.VendorId
                    )
                    : null;

            // Combine ProcOffers items
            var unitItems =
                p.ProcOffers != null && p.ProcOffers.Any()
                    ? string.Join(", ", p.ProcOffers.Select(o => o.ItemPenawaran))
                    : null;

            // Get approval dates from PR Status History
            var statusHistories = pr?.StatusHistories?.ToList() ?? new List<Core.Models.PurchaseRequisitionStatusHistory>();

            // An approval lands when the job moves to the next approver; after a
            // revision the latest pass counts.
            DateTime? ReachedAt(ProcurementStatus status) =>
                p.StatusHistories?
                    .Where(h => h.Status == status)
                    .OrderByDescending(h => h.ChangedAt)
                    .FirstOrDefault()?.ChangedAt;

            var approvalAnalystDate = ReachedAt(ProcurementStatus.WaitingApprovalAsstManager);
            var approvalAsstManagerDate =
                ReachedAt(ProcurementStatus.WaitingApprovalManager) ?? p.ManagerApprovalStartAt;

            // Tanggal Approval Manager = ketika status berubah ke OnSubmitISPA
            // Ini adalah tanggal selesai untuk dokumen RKS, BOQ, MEMO, RA
            var approvalManagerDate = p.ManagerApprovalEndAt
                ?? statusHistories
                    .FirstOrDefault(h => h.Status == PurchaseRequisitionStatus.OnSubmitISPA)?.ChangedAt;

            // Tanggal Release PR = ketika status berubah ke OnSubmitHardcopy
            var releasePrDate = statusHistories
                .FirstOrDefault(h => h.Status == PurchaseRequisitionStatus.OnSubmitHardcopy)?.ChangedAt;

            return new LdpRecapDto
            {
                ProcurementId = p.ProcurementId,

                // Basic Info
                NoWo = p.Wonum,
                NoSpk = p.SpkNumber,
                NamaVendor = selectedVendor?.VendorName,
                TglMulai = p.StartDate,
                TglSelesai = p.EndDate,
                TextPekerjaan = p.JobName,
                JobType = p.JobType?.TypeName,
                ContractType = p.ContractType.ToString(),
                LtcName = p.LtcName,

                // PR Related - use PR description as text pekerjaan PR
                TextPekerjaanPr = pr?.Description,

                // Additional Info - Flag50K computed from TextPekerjaanPr length
                Flag50K = !string.IsNullOrEmpty(pr?.Description) ? pr.Description.Length.ToString() : null,
                NoAccrual = p.NoAccrual,
                YearJob = p.StartDate.Year,
                NoRig = p.NoRig,

                // Financial
                NilaiPnl = profitLoss?.Profit,
                NilaiAccrual = p.NilaiAccrual ?? profitLoss?.AccrualAmount,
                NilaiRealisasi = p.NilaiRealisasi ?? profitLoss?.RealizationAmount,

                // Document Numbers
                NoSpmp = p.SpmpNumber,
                NoHte = p.NoHte,
                ProjectRegion = p.ProjectRegion.ToString(),
                ProjectCode = p.ProjectCode,
                LinkDokumen = procDocs.FirstOrDefault()?.ObjectKey,

                // Invoice Data
                SANo = p.SANo,
                SP3No = p.SP3No,
                PicApPo = p.PicAppo ?? p.AppoUser?.FullName,
                NoPo = p.PoNumber,
                NoRa = p.RaNumber,
                TglSubmitHardcopy = p.HardcopySubmittedAt,
                NoInvoice = p.InvoiceNumber,
                TglInvoice = p.InvoiceDate,
                TglSa = p.SaDate,
                TglSp3 = p.Sp3Date,
                TglSubmitLoket = p.LoketSubmitDate,
                PicApInvoice = p.PicApInvoice,

                // Items
                UnitItemPenawaran = unitItems,
                SuratPenawaranVendor = vendorLetter?.LetterNumber,
                Memorandum = p.MemoNumber,
                TglDoc = p.DocumentDate,

                // Keterangan - Accrual fields now mapped from Procurement
                Keterangan1 = BuildKeterangan(p),
                PotensiAccrual = p.PotensiAccrual,
                TglAccrual = p.PotentialAccrualDate,
                StatusAccrual = p.StatusAccrual,

                // Document Dates - completed when the Manager approves
                RksTglSelesai = approvalManagerDate,
                BoqTglSelesai = approvalManagerDate,
                MemoTglSelesai = approvalManagerDate,
                RaTglSelesai = approvalManagerDate,

                // Purchase Requisition
                NoPr = p.PrNumber ?? pr?.PrNumber,
                TanggalBuatPr = pr?.CreatedAt,
                TanggalRilisPr = pr?.CreatedAt, // Tanggal rilis PR = tanggal buat PR

                // Approval Dates - from PR Status History
                TanggalApprovalAnalyst = approvalAnalystDate,
                TanggalApprovalAsstManager = approvalAsstManagerDate,
                TanggalApprovalManager = approvalManagerDate,
                TanggalApprovalVp = null,
                TanggalApprovalDirektur = null,
                TanggalSubmitIspa = p.IspaSubmitDate ?? p.IspaSubmittedAt,

                // Approval Timeline (Mulai/Selesai for each level)
                ManagerApprovalSelesai = p.ManagerApprovalEndAt,
                VpApprovalSelesai = p.VpApprovalEndAt,

                // LDP Document Info
                HasLdpDocument = !string.IsNullOrEmpty(p.LdpFileObjectKey),
                LdpFileName = p.LdpFileName,
                LdpFileSize = p.LdpFileSize,
                LdpUploadedAt = p.LdpUploadedAt,
            };
        }
    }
}
