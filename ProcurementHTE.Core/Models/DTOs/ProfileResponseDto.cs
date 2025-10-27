namespace ProcurementHTE.Core.Models.DTOs
{
    public record ProfileResponseDto(
        string UserId,
        string UserName,
        string? FullName,
        string Email,
        string FirstName,
        string LastName,
        string? PhoneName,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? LastLoginAt,
        string[] Roles
    );
}
