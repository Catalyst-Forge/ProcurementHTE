namespace ProcurementHTE.Core.Interfaces;

/// <summary>The start years the month and year picker offers: only years that have jobs.</summary>
public interface IListPeriodYears
{
    /// <summary>Years of the jobs run in Docutrax, newest first.</summary>
    Task<IReadOnlyList<int>> ProcurementYearsAsync(CancellationToken ct = default);

    /// <summary>Years of the Arsip LDP rows, newest first.</summary>
    Task<IReadOnlyList<int>> ArchiveYearsAsync(CancellationToken ct = default);
}
