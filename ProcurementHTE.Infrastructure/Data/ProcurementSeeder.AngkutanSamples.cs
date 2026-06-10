using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Infrastructure.Data;

public static partial class ProcurementSeeder
{
    private static IEnumerable<ProcurementSeedData> BuildAngkutanSamples()
    {
        return new[]
        {
            new ProcurementSeedData
            {
                JobTypeName = "Angkutan",
                StatusName = "Draft",
                ContractType = ContractType.LTC,
                ProcurementCategory = ProcurementCategory.Jasa,
                JobName = "Jasa angkutan menggunakan trailer highbed untuk pengangkutan coring tools",
                SpkNumber = "1063/PROC-DS/DSI1310/2025",
                Wonum = "111/DSI1130/2025",
                StartDate = new DateTime(2025, 9, 1),
                EndDate = new DateTime(2025, 10, 15),
                ProjectRegion = ProjectRegion.SMTR,
                PotentialAccrualDate = new DateTime(2025, 11, 1),
                SpmpNumber = "SPMP-001/2025",
                MemoNumber = "MEMO-ANG-JASA/2025",
                OeNumber = "OE-ANG-JASA/2025",
                RaNumber = "RA-001/2025",
                ProjectCode = "HTE/2110/030",
                LtcName = "LTC Transport",
                Note = "Driver & kenek wajib menggunakan APD lengkap.",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                Details =
                {
                    new("Core Barrel (2 Jts 6-3/4\" Core Barrel dan 2 Jts 7\" Core Barrel) c/w protector", 4, "Jts"),
                    new("Lay Down Cradle", 2, "Ea"),
                    new("Inner Barrel Aluminium 4.3/4\" OD", 2, "Jts"),
                    new("Float Sub 6.3/4\" c/w protector (dalam tool box)", 2, "Ea"),
                }
            },
            new ProcurementSeedData
            {
                JobTypeName = "Angkutan",
                StatusName = "Draft",
                ContractType = ContractType.Spot,
                ProcurementCategory = ProcurementCategory.Barang,
                JobName = "Pengangkutan material konstruksi ke lokasi proyek Sumatra",
                SpkNumber = "1064/PROC-ANG/BAR001/2025",
                Wonum = "112/DSI1131/2025",
                StartDate = new DateTime(2025, 8, 1),
                EndDate = new DateTime(2025, 9, 30),
                ProjectRegion = ProjectRegion.JWKT,
                PotentialAccrualDate = new DateTime(2025, 10, 10),
                SpmpNumber = "SPMP-002/2025",
                MemoNumber = "MEMO-ANG-BAR/2025",
                OeNumber = "OE-ANG-BAR/2025",
                RaNumber = "RA-002/2025",
                ProjectCode = "HTE/2110/031",
                LtcName = "Material Transport",
                Note = "Material harus terlindung dari hujan selama pengangkutan",
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                Details =
                {
                    new("Semen Portland Type I", 100, "Zak"),
                    new("Besi Beton D16", 50, "Btg"),
                    new("Pasir Cor", 10, "M3"),
                    new("Batu Split 2/3", 8, "M3"),
                }
            },
        };
    }
}
