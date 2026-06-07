using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface IProcurementDocumentGenerator
{
    Task<ProcurementDocumentGenerationResult> GenerateAsync(
        string? documentTypeName,
        Procurement procurement,
        CancellationToken ct = default
    );
}
