using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Infrastructure.Data;

public static partial class ProcurementSeeder
{
    private static IEnumerable<ProcurementSeedData> BuildMovingSamples()
    {
        return new[]
        {
            new ProcurementSeedData
            {
                JobTypeName = "Moving",
                StatusName = "Draft",
                ContractType = ContractType.LTC,
                ProcurementCategory = ProcurementCategory.Jasa,
                JobName = "Jasa mobilisasi rig dan demobilisasi dari lokasi Jambi ke Riau",
                SpkNumber = "3001/PROC-MOV/JSA001/2025",
                Wonum = "115/DSI1134/2025",
                StartDate = new DateTime(2025, 5, 1),
                EndDate = new DateTime(2025, 7, 31),
                ProjectRegion = ProjectRegion.SMTR,
                PotentialAccrualDate = new DateTime(2025, 8, 15),
                SpmpNumber = "SPMP-005/2025",
                MemoNumber = "MEMO-MOV-JASA/2025",
                OeNumber = "OE-MOV-JASA/2025",
                RaNumber = "RA-005/2025",
                ProjectCode = "HTE/2110/034",
                LtcName = "Rig Mobilization",
                Note = "Termasuk escort dan izin jalan untuk heavy equipment",
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                Details =
                {
                    new("Rig Mast Transportation", 1, "Ls"),
                    new("Substructure Transport", 1, "Ls"),
                    new("Power System Transport", 1, "Ls"),
                    new("Pipe Rack Transport", 1, "Ls"),
                }
            },
            new ProcurementSeedData
            {
                JobTypeName = "Moving",
                StatusName = "Draft",
                ContractType = ContractType.Spot,
                ProcurementCategory = ProcurementCategory.Barang,
                JobName = "Mobilisasi peralatan safety dan emergency equipment",
                SpkNumber = "3002/PROC-MOV/BAR001/2025",
                Wonum = "116/DSI1135/2025",
                StartDate = new DateTime(2025, 10, 1),
                EndDate = new DateTime(2025, 11, 15),
                ProjectRegion = ProjectRegion.JWKT,
                PotentialAccrualDate = new DateTime(2025, 11, 30),
                SpmpNumber = "SPMP-006/2025",
                MemoNumber = "MEMO-MOV-BAR/2025",
                OeNumber = "OE-MOV-BAR/2025",
                RaNumber = "RA-006/2025",
                ProjectCode = "HTE/2110/035",
                LtcName = "Safety Equipment",
                Note = "Prioritas tinggi - untuk safety training karyawan baru",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Details =
                {
                    new("Fire Extinguisher CO2 9kg", 20, "Pcs"),
                    new("Emergency Stretcher", 4, "Pcs"),
                    new("First Aid Kit Complete", 10, "Set"),
                    new("Emergency Light", 15, "Pcs"),
                }
            },
        };
    }
}
