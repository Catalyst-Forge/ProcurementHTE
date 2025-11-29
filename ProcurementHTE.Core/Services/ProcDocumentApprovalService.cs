using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class ProcDocumentApprovalService : IProcDocumentApprovalService
    {
        private readonly IProcDocumentApprovalRepository _repository;

        public ProcDocumentApprovalService(IProcDocumentApprovalRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<ProcDocumentApprovals>> GetApprovedDocumentsAsync(
            string procDocumentId
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(procDocumentId))
                {
                    return new List<ProcDocumentApprovals>();
                }

                var approvals = await _repository.GetApprovedByProcDocumentIdAsync(procDocumentId);

                return approvals;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
