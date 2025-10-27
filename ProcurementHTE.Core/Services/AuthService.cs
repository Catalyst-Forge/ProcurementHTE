using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services {
    public class AuthService : IAuthService {
        private readonly IUserRepository _users;
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IJwtTokenService _jwt;
        private readonly UserManager<User> _userManager;
        private readonly int _accessMinutes;
        private readonly int _refreshDays;

        public AuthService(
            IUserRepository users,
            IRefreshTokenRepository refreshTokens,
            IJwtTokenService jwt,
            UserManager<User> userManager,
            int accessTokenMinutes = 60,
            int refreshTokenDays = 14) {
            _users = users;
            _refreshTokens = refreshTokens;
            _jwt = jwt;
            _userManager = userManager;
            _accessMinutes = accessTokenMinutes;
            _refreshDays = refreshTokenDays;
        }

        public async Task<TokenResponseDto> LoginAsync(LoginRequestDto dto, string? ip, CancellationToken ct = default) {
            var user = await _users.FindByEmailAsync(dto.Email, ct)
                       ?? throw new UnauthorizedAccessException("Username atau password salah.");

            if (!await _users.CheckPasswordAsync(user, dto.Password))
                throw new UnauthorizedAccessException("Username atau password salah.");

            // (Opsional) bersihin token kadaluarsa
            await _refreshTokens.DeleteExpiredAsync(DateTime.Now, ct);

            // Enforce single active refresh token per device:
            await _refreshTokens.DeleteAllForDeviceAsync(user.Id.ToString(), dto.DeviceId, ct);

            var access = await _jwt.GenerateTokenAsync(user);
            var expires = DateTime.Now.AddMinutes(_accessMinutes);

            var refresh = new RefreshToken {
                UserId = user.Id.ToString(),
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                DeviceId = dto.DeviceId,
                ExpiresAt = DateTime.Now.AddDays(_refreshDays),
                IpAddress = ip
            };
            await _refreshTokens.AddAsync(refresh, ct);
            await _refreshTokens.SaveAsync(ct);

            return new TokenResponseDto(access, expires, refresh.Token, refresh.ExpiresAt);
        }

        public async Task<TokenResponseDto> RefreshAsync(RefreshRequestDto dto, CancellationToken ct = default) {
            var rt = await _refreshTokens.FindByTokenAsync(dto.RefreshToken, ct)
                     ?? throw new UnauthorizedAccessException("Refresh token tidak valid.");

            if (rt.ExpiresAt <= DateTime.Now)
                throw new UnauthorizedAccessException("Refresh token kedaluwarsa.");

            if (!string.IsNullOrEmpty(dto.DeviceId) && rt.DeviceId != dto.DeviceId)
                throw new UnauthorizedAccessException("Refresh token tidak cocok dengan device.");

            var user = await _userManager.FindByIdAsync(rt.UserId)
                       ?? throw new UnauthorizedAccessException("User tidak ditemukan.");

            // Hapus token lama (rotation by hard delete)
            await _refreshTokens.DeleteByTokenAsync(rt.Token, ct);

            var newRt = new RefreshToken {
                UserId = rt.UserId,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                DeviceId = rt.DeviceId,
                ExpiresAt = DateTime.Now.AddDays(_refreshDays)
            };
            await _refreshTokens.AddAsync(newRt, ct);
            await _refreshTokens.SaveAsync(ct);

            var access = await _jwt.GenerateTokenAsync(user);
            var exp = DateTime.Now.AddMinutes(_accessMinutes);

            return new TokenResponseDto(access, exp, newRt.Token, newRt.ExpiresAt);
        }


        public async Task LogoutAsync(LogoutRequestDto dto, string userIdFromContext, CancellationToken ct = default) {
            if (dto.RevokeAllForDevice) {
                if (string.IsNullOrWhiteSpace(dto.DeviceId))
                    throw new ArgumentException("DeviceId wajib diisi saat RevokeAllForDevice=true");

                await _refreshTokens.DeleteAllForDeviceAsync(userIdFromContext, dto.DeviceId, ct);
                await _refreshTokens.SaveAsync(ct);
                return;
            }

            if (!string.IsNullOrWhiteSpace(dto.RefreshToken)) {
                // optional keamanan: pastikan token memang milik user
                var rt = await _refreshTokens.FindByTokenAsync(dto.RefreshToken, ct);
                if (rt is null || rt.UserId != userIdFromContext)
                    return;

                await _refreshTokens.DeleteByTokenAsync(dto.RefreshToken, ct);
                await _refreshTokens.SaveAsync(ct);
            }
        }


        public async Task<ProfileResponseDto> GetProfileAsync(string userId, CancellationToken ct = default) {
            var user = await _users.GetByIdAsync(userId, ct) ?? throw new UnauthorizedAccessException();
            var roles = await _users.GetRolesAsync(user);
            return new ProfileResponseDto(user.Id.ToString(), user.UserName!, user.FullName, user.Email!, user.FirstName, user.LastName, user.PhoneNumber, user.CreatedAt, user.UpdatedAt, user.LastLoginAt, roles.ToArray());
        }
    }
}
