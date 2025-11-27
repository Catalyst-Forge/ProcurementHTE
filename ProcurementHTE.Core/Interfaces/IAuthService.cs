using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IAuthService
    {
        Task<TokenResponseDto> LoginAsync(
            LoginRequestDto dto,
            string? ip,
            CancellationToken ct = default
        );
        Task<TokenResponseDto> RefreshAsync(RefreshRequestDto dto, CancellationToken ct = default);
        Task LogoutAsync(
            LogoutRequestDto dto,
            string userIdFromContext,
            CancellationToken ct = default
        );
        Task<ProfileResponseDto> GetProfileAsync(string userId, CancellationToken ct = default);
    }
}
