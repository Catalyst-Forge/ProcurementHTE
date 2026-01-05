using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

/// <summary>
/// Service untuk menangani perhitungan PNL yang berbeda per JobType
/// </summary>
public class JobTypeCalculationService : IJobTypeCalculationService
{
    // UnitType GUIDs dari seed data
    private const string UNIT_TYPE_HARI = "11111111-1111-1111-1111-111111111111";
    private const string UNIT_TYPE_JAM = "22222222-2222-2222-2222-222222222222";
    private const string UNIT_TYPE_LSP = "33333333-3333-3333-3333-333333333333";
    private const string UNIT_TYPE_TRIP = "44444444-4444-4444-4444-444444444444";
    private const string UNIT_TYPE_KALI = "55555555-5555-5555-5555-555555555555";

    // JobType TypeNames (from database)
    private const string JOBTYPE_PENGANGKUTAN = "Angkutan";
    private const string JOBTYPE_SEWA_UNIT = "StandBy";
    private const string JOBTYPE_MOVING = "Moving";

    /// <summary>
    /// Mendapatkan list UnitType yang tersedia berdasarkan JobType
    /// </summary>
    public List<string> GetAvailableUnitTypeIds(string jobTypeName)
    {
        return jobTypeName switch
        {
            JOBTYPE_PENGANGKUTAN => new List<string> { UNIT_TYPE_TRIP }, // Fixed: TRIP only
            JOBTYPE_SEWA_UNIT => new List<string> { UNIT_TYPE_HARI, UNIT_TYPE_JAM, UNIT_TYPE_LSP },
            JOBTYPE_MOVING => new List<string> { UNIT_TYPE_TRIP, UNIT_TYPE_KALI },
            _ => new List<string> { UNIT_TYPE_TRIP } // Default fallback
        };
    }

    /// <summary>
    /// Mendapatkan default UnitType untuk JobType tertentu
    /// </summary>
    public string GetDefaultUnitTypeId(string jobTypeName)
    {
        return jobTypeName switch
        {
            JOBTYPE_PENGANGKUTAN => UNIT_TYPE_TRIP,
            JOBTYPE_SEWA_UNIT => UNIT_TYPE_HARI,
            JOBTYPE_MOVING => UNIT_TYPE_TRIP,
            _ => UNIT_TYPE_TRIP
        };
    }

    /// <summary>
    /// Menghitung Revenue untuk ProfitLossItem berdasarkan JobType
    /// </summary>
    /// <param name="item">ProfitLossItem yang akan dihitung</param>
    /// <param name="jobTypeName">Nama JobType dari Procurement</param>
    /// <param name="distance">Distance dari ProfitLoss header (untuk PENGANGKUTAN)</param>
    /// <returns>Calculated revenue</returns>
    public decimal CalculateItemRevenue(ProfitLossItem item, string jobTypeName, decimal? distance)
    {
        if (jobTypeName == JOBTYPE_PENGANGKUTAN)
        {
            // DISTANCE_BASED formula: (BasePrice + OperatorCost) × UnitQty
            // OperatorCost = TarifAdd × KmPer25
            // KmPer25 = (Distance - 400) / 25

            if (!distance.HasValue || distance.Value <= 0)
            {
                throw new InvalidOperationException(
                    $"Distance wajib diisi untuk JobType {JOBTYPE_PENGANGKUTAN}"
                );
            }

            var kmPer25 = CalculateKmPer25(distance.Value);
            var tarifAdd = item.TarifAdd ?? 0;
            var operatorCost = tarifAdd * kmPer25;
            var basePrice = item.BasePrice;

            return (basePrice + operatorCost) * item.UnitQty;
        }
        else // SEWA_UNIT or MOVING - SIMPLE formula
        {
            // SIMPLE formula: BasePrice × Quantity × UnitQty
            if (!item.Quantity.HasValue || item.Quantity.Value <= 0)
            {
                throw new InvalidOperationException(
                    $"Quantity/Durasi wajib diisi untuk JobType {jobTypeName}"
                );
            }

            return item.BasePrice * item.Quantity.Value * item.UnitQty;
        }
    }

    /// <summary>
    /// Menghitung KmPer25 untuk PENGANGKUTAN
    /// Formula: (Distance - 400) / 25
    /// </summary>
    public decimal CalculateKmPer25(decimal distance)
    {
        if (distance <= 400)
            return 0;

        return (distance - 400) / 25;
    }

    /// <summary>
    /// Menghitung OperatorCost untuk PENGANGKUTAN
    /// Formula: TarifAdd × KmPer25
    /// </summary>
    public decimal CalculateOperatorCost(decimal tarifAdd, decimal kmPer25)
    {
        return tarifAdd * kmPer25;
    }

    /// <summary>
    /// Menghitung total vendor cost berdasarkan penawaran vendor
    /// </summary>
    /// <param name="offers">List VendorOffer untuk vendor tertentu</param>
    /// <returns>Total vendor cost (best price per item × quantity × unit)</returns>
    public decimal CalculateVendorTotal(List<VendorOffer> offers)
    {
        return offers
            .GroupBy(o => o.ProcOfferId)
            .Sum(g =>
            {
                // Ambil harga terendah dari semua round
                var bestPrice = g.Min(o => o.Price);
                var representative = g.First();

                return bestPrice * representative.QuantityItem * representative.QuantityOfUnit;
            });
    }

    /// <summary>
    /// Validasi field yang required untuk JobType tertentu
    /// </summary>
    public void ValidateRequiredFields(ProfitLoss profitLoss, string jobTypeName)
    {
        switch (jobTypeName)
        {
            case JOBTYPE_PENGANGKUTAN:
                if (!profitLoss.Distance.HasValue || profitLoss.Distance.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "Distance wajib diisi untuk Angkutan"
                    );
                }
                break;

            case JOBTYPE_SEWA_UNIT:
                if (!profitLoss.TglMulaiSewa.HasValue)
                {
                    throw new InvalidOperationException(
                        "Tanggal Mulai Sewa wajib diisi untuk Sewa Unit"
                    );
                }
                break;

            case JOBTYPE_MOVING:
                if (!profitLoss.TglMulaiMoving.HasValue)
                {
                    throw new InvalidOperationException(
                        "Tanggal Mulai Moving wajib diisi untuk Moving"
                    );
                }
                break;
        }
    }

    /// <summary>
    /// Mendapatkan UnitTypeId untuk VendorOffer berdasarkan JobType
    /// </summary>
    /// <param name="jobTypeName">Nama JobType</param>
    /// <param name="billingUnitTypeId">UnitTypeId dari ProfitLossItem (untuk SEWA_UNIT & MOVING)</param>
    /// <returns>UnitTypeId yang sesuai</returns>
    public string GetVendorOfferUnitTypeId(string jobTypeName, string? billingUnitTypeId)
    {
        if (jobTypeName == JOBTYPE_PENGANGKUTAN)
        {
            // PENGANGKUTAN always uses TRIP
            return UNIT_TYPE_TRIP;
        }
        else
        {
            // SEWA_UNIT & MOVING inherit from billing
            return billingUnitTypeId ?? GetDefaultUnitTypeId(jobTypeName);
        }
    }

    /// <summary>
    /// Cek apakah field Distance digunakan untuk JobType ini
    /// </summary>
    public bool IsDistanceUsed(string jobTypeName)
    {
        return jobTypeName switch
        {
            JOBTYPE_PENGANGKUTAN => true,  // Required for calculation
            JOBTYPE_MOVING => true,        // Info only (not for calculation)
            _ => false
        };
    }

    /// <summary>
    /// Cek apakah field Distance required untuk calculation
    /// </summary>
    public bool IsDistanceRequiredForCalculation(string jobTypeName)
    {
        return jobTypeName == JOBTYPE_PENGANGKUTAN;
    }
}
