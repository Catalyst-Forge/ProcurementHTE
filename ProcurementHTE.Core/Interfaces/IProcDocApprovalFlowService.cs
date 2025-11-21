namespace ProcurementHTE.Core.Interfaces {
    public interface IProcDocApprovalFlowService {
        // extraRoleNames: optional role names (e.g. "Vice President") to append to the generated flow
        Task GenerateFlowAsync(string woId, string procDocumentId, IEnumerable<string>? extraRoleNames = null);
    }
}
