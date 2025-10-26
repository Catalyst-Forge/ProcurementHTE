namespace ProcurementHTE.Core.Interfaces {
    public interface IWoDocApprovalFlowService {
        Task GenerateFlowAsync(string woId, string woDocumentId);
    }
}
