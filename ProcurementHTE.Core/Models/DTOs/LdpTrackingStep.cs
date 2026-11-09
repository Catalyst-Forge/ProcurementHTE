namespace ProcurementHTE.Core.Models.DTOs;

/// <summary>One stage of an archived job, with the date the spreadsheet recorded for it, if any.</summary>
public sealed record LdpTrackingStep(string Label, DateTime? Date)
{
    public bool Done => Date is not null;
}
