namespace ProcurementHTE.Core.Models.DTOs
{
    public class LdpRecapDto
    {
        // Identifiers
        public string ProcurementId { get; set; } = null!;

        // Basic Info
        public string? NoWo { get; set; }
        public string? NoSpk { get; set; }
        public string? NamaVendor { get; set; }
        public DateTime? TglMulai { get; set; }
        public DateTime? TglSelesai { get; set; }
        public string? TextPekerjaan { get; set; }
        public string? JobType { get; set; }
        public string? ContractType { get; set; }
        public string? LtcName { get; set; }

        // PR Related
        public string? TextPekerjaanPr { get; set; }

        // Additional Info
        public string? Flag50K { get; set; }
        public string? NoAccrual { get; set; }
        public int? YearJob { get; set; }
        public string? NoRig { get; set; }

        // Financial
        public decimal? NilaiPnl { get; set; }
        public decimal? NilaiAccrual { get; set; }
        public decimal? NilaiRealisasi { get; set; }

        // Document Numbers
        public string? NoSpmp { get; set; }
        public string? NoHte { get; set; }
        public string? ProjectRegion { get; set; }
        public string? ProjectCode { get; set; }
        public string? LinkDokumen { get; set; }

        // Items
        public string? UnitItemPenawaran { get; set; }
        public string? SuratPenawaranVendor { get; set; }
        public string? Memorandum { get; set; }
        public DateTime? TglDoc { get; set; }

        // Keterangan
        public string? Keterangan1 { get; set; }
        public decimal? PotensiAccrual { get; set; }
        public DateTime? TglAccrual { get; set; }
        public string? StatusAccrual { get; set; }

        // Document Dates (RKS, BOQ, Memo, RA)
        public DateTime? RksTglMulai { get; set; }
        public DateTime? RksTglSelesai { get; set; }
        public DateTime? BoqTglMulai { get; set; }
        public DateTime? BoqTglSelesai { get; set; }
        public DateTime? MemoTglMulai { get; set; }
        public DateTime? MemoTglSelesai { get; set; }
        public DateTime? RaTglMulai { get; set; }
        public DateTime? RaTglSelesai { get; set; }

        // Purchase Requisition
        public string? NoPr { get; set; }
        public DateTime? TanggalBuatPr { get; set; }
        public DateTime? TanggalRilisPr { get; set; }

        // Approval Dates
        public DateTime? TanggalApprovalOps { get; set; }
        public DateTime? TanggalApprovalManager { get; set; }
        public DateTime? TanggalApprovalVp { get; set; }
        public DateTime? TanggalApprovalDirektur { get; set; }
        public DateTime? TanggalSubmitIspa { get; set; }
    }
}
