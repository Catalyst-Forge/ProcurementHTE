using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Infrastructure.Data;

public static partial class ProcurementSeeder
{
    private static IEnumerable<ProcurementSeedData> BuildStandBySamples()
    {
        return new[]
        {
            new ProcurementSeedData
            {
                JobTypeName = "StandBy",
                StatusName = "Draft",
                ContractType = ContractType.RO,
                ProcurementCategory = ProcurementCategory.Jasa,
                JobName = "Sewa unit excavator untuk proyek pemboran geothermal",
                SpkNumber = "2001/PROC-STB/JSA001/2025",
                Wonum = "113/DSI1132/2025",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 12, 31),
                ProjectRegion = ProjectRegion.SMTR,
                PotentialAccrualDate = new DateTime(2026, 1, 15),
                SpmpNumber = "SPMP-003/2025",
                MemoNumber = "MEMO-STB-JASA/2025",
                OeNumber = "OE-STB-JASA/2025",
                RaNumber = "RA-003/2025",
                ProjectCode = "HTE/2110/032",
                LtcName = "Equipment Rental",
                Note = "Termasuk operator bersertifikat dan fuel",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                Details =
                {
                    new("Excavator Komatsu PC200", 1, "Pcs"),
                    new("Operator bersertifikat", 2, "Org"),
                    new("Fuel Support", 1, "Ls"),
                    new("Maintenance Kit", 1, "Set"),
                }
            },
            new ProcurementSeedData
            {
                JobTypeName = "StandBy",
                StatusName = "Draft",
                ContractType = ContractType.STB,
                ProcurementCategory = ProcurementCategory.Barang,
                JobName = "Sewa generator standby untuk base camp operasional",
                SpkNumber = "2002/PROC-STB/BAR001/2025",
                Wonum = "114/DSI1133/2025",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 11, 30),
                ProjectRegion = ProjectRegion.AKOMODASI,
                PotentialAccrualDate = new DateTime(2025, 12, 15),
                SpmpNumber = "SPMP-004/2025",
                MemoNumber = "MEMO-STB-BAR/2025",
                OeNumber = "OE-STB-BAR/2025",
                RaNumber = "RA-004/2025",
                ProjectCode = "HTE/2110/033",
                LtcName = "Power Support",
                Note = "Termasuk instalasi dan kabel power",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                Details =
                {
                    new("Generator 150 KVA", 2, "Pcs"),
                    new("Kabel Power 50m", 4, "Roll"),
                    new("Panel Distribution Board", 2, "Pcs"),
                    new("Fuel Tank 1000L", 2, "Pcs"),
                }
            },
        };
    }
}
