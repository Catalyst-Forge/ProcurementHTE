using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data;

public static class VendorSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Vendors.AnyAsync())
            return;

        var vendors = new (string Code, string Name)[]
        {
            ("VND0002", "PT Krakatau Jasa Logistik"),
            ("VND0003", "PT Serasi Logistics Indonesia"),
            ("VND0004", "PT Eximku Teknologi Indonesia"),
            ("VND0005", "PT Pancaran Energi Transportasi"),
            ("VND0006", "PT Permata Lintas Buana"),
            ("VND0007", "PT Cipta Hasil Sugiarto"),
            ("VND0008", "PT.Mandiri Trans Utama"),
            ("VND0009", "PT.Logistic One Solution"),
            ("VND0010", "PT Sahindo Manggala Sejahtera"),
            ("VND0011", "PT Subur Sedaya Maju"),
            ("VND0012", "PT Cahaya Mulia Adhilestari"),
            ("VND0013", "PT.Pos Logistik"),
            ("VND0014", "PT Zasa Lestari Mustika"),
            ("VND0015", "PT Permata Lintas Buana"),
            ("VND0016", "PT Rezeki Prayogo Abadi"),
            ("VND0017", "PT Eximku Logistic Indonesia"),
            ("VND0018", "PT Arpilog"),
            ("VND0019", "PT Eximku Teknologi Indonesia"),
            ("VND0020", "PT Samudera Bandar Logistik"),
            ("VND0021", "PT Orindo Nusantara Energi"),
            ("VND0022", "PT Patra Logistik"),
            ("VND0023", "PT Maritim Transport"),
            ("VND0024", "PT  MTN Cargo"),
            ("VND0025", "PT Neo Trans Logistik"),
            ("VND0026", "PT Citra Paseh Indah"),
            ("VND0027", "PT Artha Muat Graha"),
            ("VND0028", "CV Lentera Jaya persada"),
            ("VND0029", "PT Krakatau Jasa Samudera"),
            ("VND0030", "PT Reksa Mandiri"),
            ("VND0031", "PT emitraco"),
            ("VND0032", "PT Seis Anara Indonesia"),
            ("VND0033", "PT TMI"),
            ("VND0034", "PT TMI"),
            ("VND0035", "PT TMITMI"),
            ("VND0036", "PT TMITMITMI"),
            ("VND0037", "PT TMITMITMITMI"),
            ("VND0038", "PT TMIII"),
            ("VND0039", "PT TMIIII"),
            ("VND0040", "PT TMII"),
            ("VND0041", "PT TMIIIII"),
            ("VND0042", "PT TMIUU"),
            ("VND0043", "PT TMIUUU"),
        };

        string DummyEmail(string code) => $"{code.ToLowerInvariant()}@example.com";

        var vendorEntities = vendors.Select(v => new Vendor
        {
            VendorCode = v.Code,
            VendorName = v.Name,
            NPWP = "00.000.000.0-000.000",
            Address = "Jl. Dummy No.1",
            City = "Jakarta",
            Province = "DKI Jakarta",
            PostalCode = 10000,
            Email = DummyEmail(v.Code),
            Comment = string.Empty,
            CreatedAt = DateTime.UtcNow,
        });

        await db.Vendors.AddRangeAsync(vendorEntities);
        await db.SaveChangesAsync();
    }
}
