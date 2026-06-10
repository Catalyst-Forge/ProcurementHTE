using System.Collections.Generic;
using System.Linq;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class LdpRepository
    {
        private static LdpRecapDto MapToDto(Core.Models.Procurement p, List<Core.Models.VendorRoundLetter> vendorRoundLetters)
        {
            var profitLoss = p.ProfitLosses.FirstOrDefault();
            var selectedVendor = profitLoss?.SelectedVendor;
            var pr = p.PurchaseRequisition;

            // Get document dates by type name
            var procDocs = p.ProcDocuments ?? new List<Core.Models.ProcDocuments>();
            var rksDoc = procDocs.FirstOrDefault(d =>
                d.DocumentType?.Name?.ToUpper().Contains("RKS") == true
            );
            var boqDoc = procDocs.FirstOrDefault(d =>
                d.DocumentType?.Name?.ToUpper().Contains("BOQ") == true
            );
            var memoDoc = procDocs.FirstOrDefault(d =>
                d.DocumentType?.Name?.ToUpper().Contains("MEMO") == true
            );
            var raDoc = procDocs.FirstOrDefault(d =>
                d.DocumentType?.Name?.ToUpper().Contains("RA") == true
            );

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

            // Tanggal Approval Analyst = ketika status berubah ke WaitingApprovalAsstManager
            var approvalAnalystDate = statusHistories
                .FirstOrDefault(h => h.Status == PurchaseRequisitionStatus.WaitingApprovalAsstManager)?.ChangedAt;

            // Tanggal Approval Asst Manager = ketika status berubah ke WaitingApprovalManager  
            var approvalAsstManagerDate = statusHistories
                .FirstOrDefault(h => h.Status == PurchaseRequisitionStatus.WaitingApprovalManager)?.ChangedAt;

            // Tanggal Approval Manager = ketika status berubah ke OnSubmitISPA
            // Ini adalah tanggal selesai untuk dokumen RKS, BOQ, MEMO, RA
            var approvalManagerDate = statusHistories
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
                NilaiAccrual = profitLoss?.AccrualAmount,
                NilaiRealisasi = profitLoss?.RealizationAmount,

                // Document Numbers
                NoSpmp = p.SpmpNumber,
                NoHte = p.NoHte,
                ProjectRegion = p.ProjectRegion.ToString(),
                ProjectCode = p.ProjectCode,
                LinkDokumen = procDocs.FirstOrDefault()?.ObjectKey,

                // Invoice Data
                SANo = p.SANo,
                SP3No = p.SP3No,

                // Items
                UnitItemPenawaran = unitItems,
                SuratPenawaranVendor = vendorLetter?.LetterNumber,
                Memorandum = p.MemoNumber,
                TglDoc = p.DocumentDate,

                // Keterangan - Accrual fields now mapped from Procurement
                Keterangan1 = p.Note,
                PotensiAccrual = p.PotensiAccrual,
                TglAccrual = p.PotentialAccrualDate,
                StatusAccrual = p.StatusAccrual,

                // Document Dates - CreatedAt as start date, Manager approval as end date
                RksTglMulai = rksDoc?.CreatedAt,
                RksTglSelesai = approvalManagerDate,
                BoqTglMulai = boqDoc?.CreatedAt,
                BoqTglSelesai = approvalManagerDate,
                MemoTglMulai = memoDoc?.CreatedAt,
                MemoTglSelesai = approvalManagerDate,
                RaTglMulai = raDoc?.CreatedAt,
                RaTglSelesai = approvalManagerDate,

                // Purchase Requisition
                NoPr = pr?.PrNumber,
                TanggalBuatPr = pr?.CreatedAt,
                TanggalRilisPr = pr?.CreatedAt, // Tanggal rilis PR = tanggal buat PR

                // Approval Dates - from PR Status History
                TanggalApprovalOps = approvalAnalystDate,
                TanggalApprovalManager = approvalManagerDate,
                TanggalApprovalVp = null,
                TanggalApprovalDirektur = null,
                TanggalSubmitIspa = pr?.IspaSubmittedAt,

                // Approval Timeline (Mulai/Selesai for each level)
                ManagerApprovalMulai = p.ManagerApprovalStartAt,
                ManagerApprovalSelesai = p.ManagerApprovalEndAt,
                VpApprovalMulai = p.VpApprovalStartAt,
                VpApprovalSelesai = p.VpApprovalEndAt,
                OpDirApprovalMulai = p.OpDirApprovalStartAt,
                OpDirApprovalSelesai = p.OpDirApprovalEndAt,
                PresDirApprovalMulai = p.PresDirApprovalStartAt,
                PresDirApprovalSelesai = p.PresDirApprovalEndAt,

                // LDP Document Info
                HasLdpDocument = !string.IsNullOrEmpty(p.LdpFileObjectKey),
                LdpFileName = p.LdpFileName,
                LdpFileSize = p.LdpFileSize,
                LdpUploadedAt = p.LdpUploadedAt,
            };
        }
    }
}
