namespace ProcurementHTE.Core.Services;

/// <summary>
/// The process dates of a spreadsheet job that shows in the LDP. Such a job already has a PO, so
/// every step up to Submit INPRO is done, one day after another, by
/// <see cref="MaxDaysToInpro"/> days after the job started (Diah, 11 Nov 2026). The hard copy
/// keeps its recorded date when it is not before INPRO. This applies to imported spreadsheet rows
/// only: a job run in Docutrax keeps the dates its people signed, and an Admin may still edit an
/// archive row's dates by hand.
/// </summary>
public static class LdpProcessSchedule
{
    public const int MaxDaysToInpro = 3;

    public static LdpProcessDates For(DateTime start, DateTime? recordedHardCopy)
    {
        var day = start.Date;
        var inpro = day.AddDays(MaxDaysToInpro);
        return new LdpProcessDates(
            PrCreated: day,
            PrReleased: day.AddDays(1),
            AnalystApproved: day.AddDays(1),
            AsstManagerApproved: day.AddDays(2),
            ManagerApproved: day.AddDays(2),
            InproSubmitted: inpro,
            HardCopySubmitted: recordedHardCopy is { } recorded && recorded.Date >= inpro ? recorded : inpro
        );
    }
}

public sealed record LdpProcessDates(
    DateTime PrCreated,
    DateTime PrReleased,
    DateTime AnalystApproved,
    DateTime AsstManagerApproved,
    DateTime ManagerApproved,
    DateTime InproSubmitted,
    DateTime HardCopySubmitted
);
