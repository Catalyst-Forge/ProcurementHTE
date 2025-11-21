namespace ProcurementHTE.Web.Models.ViewModels
{
    public class ProcurementDocumentStatsViewModel
    {
        public int CompletedDocs { get; init; }
        public int RemainingDocs { get; init; }
        public int GeneratedDocs { get; init; }
        public int MandatoryDocs { get; init; }
        public int ApprovalDocs { get; init; }
        public int TotalDocs { get; init; }
    }
}
