namespace ProcurementHTE.Core.Models.DTOs;

/// <summary>
/// One PR number can back many PR records and many jobs. Each vendor payment draws its
/// realization from the same PR value.
/// </summary>
public sealed record PrBalance(string PrNumber, decimal? Value, decimal Used, int Jobs)
{
    public decimal? Remaining => Value - Used;

    public bool IsOverdrawn => Value is { } value && Used > value;

    public string? OverdrawnWarning =>
        IsOverdrawn
            ? $"Tersimpan, tetapi realisasi PR {PrNumber} sudah Rp {Used:N0}, melebihi nilai PR Rp {Value:N0} (lebih Rp {Used - Value:N0})."
            : null;
}
