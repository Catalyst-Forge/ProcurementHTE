using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Infrastructure.Data
{
    public static partial class JobTypeMovingMobilizationSeeder
    {
        private static readonly (
            string Name,
            int Seq,
            bool Mandatory,
            bool Generated,
            bool UploadReq,
            bool RequiresApproval,
            string? Note,
            ProcurementCategory? Category
        )[] ConfigDocuments = new (
            string Name,
            int Seq,
            bool Mandatory,
            bool Generated,
            bool UploadReq,
            bool RequiresApproval,
            string? Note,
            ProcurementCategory? Category
        )[]
        {
            (
                "Permintaan Pekerjaan",
                1,
                true,
                false,
                true,
                false,
                "Dokumen eksternal; tidak di-generate; tidak perlu approval",
                null
            ),
            (
                "Profit & Loss",
                2,
                true,
                true,
                false,
                true,
                "Di-generate sistem; approval Analyst HTE & LTS -> AM HTE -> Manager",
                null
            ),
            (
                "Surat Penawaran Harga",
                3,
                false,
                false,
                false,
                false,
                "Selalu ada, dikelola via menu Documents (bukan JobType config)",
                null
            ),
            (
                "Surat Negosiasi Harga",
                4,
                false,
                false,
                false,
                false,
                "Selalu ada, dikelola via menu Documents (bukan JobType config)",
                null
            ),
            (
                "Surat Perintah Mulai Pekerjaan (SPMP)",
                5,
                true,
                true,
                false,
                true,
                "SPMP upload dengan approval Manager",
                null
            ),
            (
                "Bill of Quantity (BOQ)",
                6,
                true,
                true,
                false,
                true,
                "BOQ digenerate otomatis oleh sistem",
                null
            ),
            (
                "Owner Estimate (OE)",
                7,
                true,
                true,
                false,
                true,
                "Di-generate sistem; approval AM HTE -> Manager",
                null
            ),
            (
                "Memorandum",
                8,
                true,
                true,
                false,
                true,
                "Memorandum upload dengan approval Manager",
                null
            ),
            (
                "Rencana Kerja dan Syarat-Syarat (RKS)",
                9,
                true,
                true,
                false,
                true,
                "Di-generate sistem; approval AM HTE -> Manager",
                ProcurementCategory.Jasa
            ),
            (
                "Risk Assessment (RA)",
                10,
                true,
                true,
                false,
                true,
                "Upload (khusus pengadaan jasa); approval HSE -> AM HTE -> Manager",
                ProcurementCategory.Jasa
            ),
            (
                "Market Survey",
                11,
                true,
                false,
                true,
                false,
                "Upload dari HTE; mempengaruhi progress",
                null
            ),
            (
                "Justifikasi",
                12,
                true,
                true,
                false,
                true,
                "Justifikasi (>=300jt, approval kondisional saat generate flow)",
                null
            ),
        };

        private static readonly (string Doc, (string Role, int Level)[] Steps)[] ApprovalMatrix = new (string Doc, (string Role, int Level)[] Steps)[]
        {
            ("Memorandum", new[] { ("Manager Transport & Logistic", 1) }),
            (
                "Surat Perintah Mulai Pekerjaan (SPMP)",
                new[] { ("Manager Transport & Logistic", 1) }
            ),
            (
                "Profit & Loss",
                new[]
                {
                    ("Analyst HTE & LTS", 1),
                    ("Assistant Manager HTE", 2),
                    ("Manager Transport & Logistic", 3),
                }
            ),
            (
                "Rencana Kerja dan Syarat-Syarat (RKS)",
                new[] { ("Assistant Manager HTE", 1), ("Manager Transport & Logistic", 2) }
            ),
            (
                "Risk Assessment (RA)",
                new[]
                {
                    ("HSE", 1),
                    ("Assistant Manager HTE", 2),
                    ("Manager Transport & Logistic", 3),
                }
            ),
            (
                "Owner Estimate (OE)",
                new[] { ("Assistant Manager HTE", 1), ("Manager Transport & Logistic", 2) }
            ),
        };
    }
}
