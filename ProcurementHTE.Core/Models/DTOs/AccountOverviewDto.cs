using ProcurementHTE.Core.Models.Enums;

namespace ProcurementHTE.Core.Models.DTOs
{
    public record AccountOverviewDto(
        string UserId,
        string UserName,
        string Email,
        string FirstName,
        string LastName,
        string? JobTitle,
        string? PhoneNumber,
        string? AvatarUrl,
        bool PhoneNumberConfirmed,
        bool EmailConfirmed,
        bool TwoFactorEnabled,
        TwoFactorMethod TwoFactorMethod,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? LastLoginAt,
        DateTime? PasswordChangedAt,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string>? RecoveryCodesSnapshot,
        bool RecoveryCodesHidden,
        DateTime? RecoveryCodesGeneratedAt
    );
}
