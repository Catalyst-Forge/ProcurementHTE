using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data;

public static class UnitTypeSeeder
{
    public static void SeedUnitTypes(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<UnitType>().HasData(
            new UnitType
            {
                UnitTypeId = "11111111-1111-1111-1111-111111111111",
                Code = "HARI",
                Name = "Hari",
                IsActive = true,
                SortOrder = 1,
                CreatedAt = seedDate
            },
            new UnitType
            {
                UnitTypeId = "22222222-2222-2222-2222-222222222222",
                Code = "JAM",
                Name = "Jam",
                IsActive = true,
                SortOrder = 2,
                CreatedAt = seedDate
            },
            new UnitType
            {
                UnitTypeId = "33333333-3333-3333-3333-333333333333",
                Code = "LSP",
                Name = "Lumpsum",
                IsActive = true,
                SortOrder = 3,
                CreatedAt = seedDate
            },
            new UnitType
            {
                UnitTypeId = "44444444-4444-4444-4444-444444444444",
                Code = "TRIP",
                Name = "Trip",
                IsActive = true,
                SortOrder = 4,
                CreatedAt = seedDate
            },
            new UnitType
            {
                UnitTypeId = "55555555-5555-5555-5555-555555555555",
                Code = "KALI",
                Name = "Kali",
                IsActive = true,
                SortOrder = 5,
                CreatedAt = seedDate
            }
        );
    }
}
