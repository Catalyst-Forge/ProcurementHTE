namespace ProcurementHTE.Core.Models.DTOs;

public static class DocStatuses
{
    public const string Uploaded = "Uploaded";
    public const string PendingApproval = "Pending Approval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Replaced = "Replaced";
    public const string Deleted = "Deleted";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Uploaded, PendingApproval, Approved, Rejected, Replaced, Deleted
    };

    public static bool IsFinal(string s) =>
        s.Equals(Approved, StringComparison.OrdinalIgnoreCase) ||
        s.Equals(Rejected, StringComparison.OrdinalIgnoreCase);
}