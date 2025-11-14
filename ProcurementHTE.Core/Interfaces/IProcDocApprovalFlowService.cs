namespace ProcurementHTE.Core.Interfaces {
    public interface IProcDocApprovalFlowService {
        Task GenerateFlowAsync(string woId, string procDocumentId);
    }
}
