using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface IWorkOrderDocumentQuery
{
    Task<WorkOrderRequiredDocsDto?> GetRequiredDocsAsync(string workOrderId, TimeSpan? presignExpiry = null);
}
