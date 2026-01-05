using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

/// <summary>
/// Interface untuk service perhitungan PNL berdasarkan JobType
/// </summary>
public interface IJobTypeCalculationService
{
    /// <summary>
    /// Mendapatkan list UnitType IDs yang tersedia untuk JobType tertentu
    /// </summary>
    List<string> GetAvailableUnitTypeIds(string jobTypeName);

    /// <summary>
    /// Mendapatkan default UnitType ID untuk JobType tertentu
    /// </summary>
    string GetDefaultUnitTypeId(string jobTypeName);

    /// <summary>
    /// Menghitung Revenue untuk ProfitLossItem berdasarkan JobType
    /// </summary>
    decimal CalculateItemRevenue(ProfitLossItem item, string jobTypeName, decimal? distance);

    /// <summary>
    /// Menghitung KmPer25 untuk PENGANGKUTAN
    /// </summary>
    decimal CalculateKmPer25(decimal distance);

    /// <summary>
    /// Menghitung OperatorCost untuk PENGANGKUTAN
    /// </summary>
    decimal CalculateOperatorCost(decimal tarifAdd, decimal kmPer25);

    /// <summary>
    /// Menghitung total vendor cost dari list VendorOffer
    /// </summary>
    decimal CalculateVendorTotal(List<VendorOffer> offers);

    /// <summary>
    /// Validasi field yang required untuk JobType tertentu
    /// </summary>
    void ValidateRequiredFields(ProfitLoss profitLoss, string jobTypeName);

    /// <summary>
    /// Mendapatkan UnitTypeId untuk VendorOffer berdasarkan JobType
    /// </summary>
    string GetVendorOfferUnitTypeId(string jobTypeName, string? billingUnitTypeId);

    /// <summary>
    /// Cek apakah Distance digunakan untuk JobType ini
    /// </summary>
    bool IsDistanceUsed(string jobTypeName);

    /// <summary>
    /// Cek apakah Distance required untuk calculation
    /// </summary>
    bool IsDistanceRequiredForCalculation(string jobTypeName);
}
