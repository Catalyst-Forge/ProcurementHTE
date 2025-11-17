using ProcurementHTE.Core.Models.Enums;

namespace ProcurementHTE.Core.Models.DTOs
{
    public record TwoFactorSummaryDto(
        bool IsEnabled,
        TwoFactorMethod Method,
        int RecoveryCodesLeft,
        string? SharedKey,
        string? AuthenticatorUri,
        string? AuthenticatorQrBase64
    );
}
