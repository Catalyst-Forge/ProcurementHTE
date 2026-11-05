using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Models.DTOs;

public sealed record PrSubmitState(string PrId, string? PrNumber, ProcurementStatus? Step);

public sealed record PrSubmitActor(string UserId, bool IsAdmin);

public sealed record PrFileUpload(string FileName, string ContentType, long Size, Stream Content);

public sealed record SimpleProfitLossDto(
    string ProcurementId,
    string ProcOfferId,
    int UnitQty,
    string VendorId,
    decimal Revenue,
    decimal Cost
);
