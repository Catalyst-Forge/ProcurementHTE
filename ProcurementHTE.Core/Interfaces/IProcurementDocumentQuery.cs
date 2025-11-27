using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface IProcurementDocumentQuery
{
    Task<ProcurementRequiredDocsDto?> GetRequiredDocsAsync(
        string procurementId,
        TimeSpan? presignExpiry = null
    );
}
