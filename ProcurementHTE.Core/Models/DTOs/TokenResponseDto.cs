namespace ProcurementHTE.Core.Models.DTOs
{
    public record TokenResponseDto(
        string AccessToken,
        DateTime ExpiresAt,
        string RefreshToken,
        DateTime RefreshExpiresAt
    );
}
