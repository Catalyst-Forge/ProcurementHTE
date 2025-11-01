using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Web.Controllers.ApiController
{
    [ApiController]
    [Area("Api")]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly JwtSettings _jwtSettings;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IOptions<JwtSettings> jwtSettings,
            IRefreshTokenRepository refreshTokenRepository,
            ILogger<AuthController> logger
        )
        {
            _authService = authService;
            _jwtSettings = jwtSettings.Value;
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login(
            [FromBody] LoginRequestDto model,
            CancellationToken ct
        )
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(
                        new LoginResponseDto { Success = false, Message = "Data tidak valid" }
                    );
                }

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var tokenResponse = await _authService.LoginAsync(model, ip, ct);

                var userId = GetUserIdFromAccessToken(tokenResponse.AccessToken);
                var profile = await _authService.GetProfileAsync(userId, ct);

                _logger.LogInformation("User {Email} logged in via API", model.Email);

                var expiresAt = DateTime.Now.AddMinutes(_jwtSettings.ExpirationInMinutes);

                return Ok(
                    new LoginResponseDto
                    {
                        Success = true,
                        Message = "Login berhasil",
                        Token = tokenResponse.AccessToken,
                        ExpiresAt = expiresAt,
                        RefreshToken = tokenResponse.RefreshToken,
                        RefreshExpiresAt = tokenResponse.RefreshExpiresAt,
                        User = new UserDataDto
                        {
                            Id = profile.UserId.ToString(),
                            UserName = profile.UserName!,
                            Email = profile.Email!,
                            FullName = profile.FullName!,
                            FirstName = profile.FirstName,
                            LastName = profile.LastName,
                            PhoneNumber = profile.PhoneName,
                            CreatedAt = profile.CreatedAt,
                            UpdatedAt = profile.UpdatedAt,
                            LastLoginAt = profile.LastLoginAt,
                            Roles = profile.Roles.ToList(),
                        },
                    }
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Login gagal untuk {Identifier}", model.Email);
                return Unauthorized(
                    new LoginResponseDto
                    {
                        Success = false,
                        Message = "Email/username atau password salah",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API login for {Email}", model.Email);
                return StatusCode(
                    500,
                    new LoginResponseDto
                    {
                        Success = false,
                        Message = "Terjadi kesalahan pada server",
                    }
                );
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> Refresh(
            [FromBody] RefreshRequestDto dto,
            CancellationToken ct
        )
        {
            try
            {
                var res = await _authService.RefreshAsync(dto, ct);
                return Ok(res);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Refresh token invalid");
                return Unauthorized(new { success = false, message = "Refresh token tidak valid" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(
                    500,
                    new { success = false, message = "Terjadi kesalahan pada server" }
                );
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(
            [FromBody] LogoutRequestDto dto,
            CancellationToken ct
        )
        {
            try
            {
                if (
                    dto is null
                    || (
                        string.IsNullOrWhiteSpace(dto.RefreshToken)
                        && !(dto.RevokeAllForDevice && !string.IsNullOrWhiteSpace(dto.DeviceId))
                    )
                )
                {
                    return BadRequest(
                        new
                        {
                            success = false,
                            message = "Kirim RefreshToken, atau RevokeAllForDevice=true + DeviceId.",
                        }
                    );
                }

                var userId =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? string.Empty;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "Token tidak valid" });

                await _authService.LogoutAsync(dto, userId, ct);

                return Ok(
                    new
                    {
                        success = true,
                        message = dto.RevokeAllForDevice
                            ? "Logout berhasil. Semua refresh token untuk device ini dihapus."
                            : "Logout berhasil. Refresh token dihapus.",
                        timestampUtc = DateTime.UtcNow,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(
                    500,
                    new { success = false, message = "Terjadi kesalahan pada server" }
                );
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserDataDto>> GetProfile(CancellationToken ct)
        {
            try
            {
                var userId = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Token tidak valid");
                    return Unauthorized(new { message = "Token tidak valid" });
                }
                var profile = await _authService.GetProfileAsync(userId, ct);

                return Ok(
                    new UserDataDto
                    {
                        Id = profile.UserId.ToString(),
                        UserName = profile.UserName!,
                        Email = profile.Email!,
                        FullName = profile.FullName!,
                        FirstName = profile.FirstName,
                        LastName = profile.LastName,
                        PhoneNumber = profile.PhoneName,
                        CreatedAt = profile.CreatedAt,
                        UpdatedAt = profile.UpdatedAt,
                        LastLoginAt = profile.LastLoginAt,
                        Roles = profile.Roles.ToList(),
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new { message = "Terjadi kesalahan pada server" });
            }
        }

        [HttpGet("validate")]
        [Authorize]
        public async Task<IActionResult> ValidateToken(
            [FromQuery] string? deviceId,
            CancellationToken ct
        )
        {
            // Jika sampai sini, berarti JWT sudah lolos verifikasi kriptografi & expiry oleh middleware.
            var userId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(
                    new
                    {
                        valid = false,
                        message = "Token tidak valid",
                        reason = "NoUserIdClaim",
                        timestamp = DateTime.Now,
                    }
                );
            }

            // Jika deviceId dikirim, cek apakah masih ada refresh token aktif untuk device tsb.
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                var hasActive = await _refreshTokenRepository.HasActiveTokenForDeviceAsync(
                    userId,
                    deviceId,
                    ct
                );
                if (!hasActive)
                {
                    return Ok(
                        new
                        {
                            valid = false,
                            message = "Token dianggap tidak valid: sesi device sudah logout.",
                            reason = "NoActiveRefreshTokenForDevice",
                            userId,
                            deviceId,
                            timestamp = DateTime.Now,
                        }
                    );
                }
            }

            // Tanpa deviceId, kita tidak bisa memastikan status logout device-level.
            var userName = User.Identity?.Name;
            return Ok(
                new
                {
                    valid = true,
                    message = "Token valid",
                    userId,
                    userName,
                    deviceId = deviceId ?? null,
                    timestamp = DateTime.Now,
                }
            );
        }

        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            return Ok(
                new
                {
                    message = "Mobile API is working",
                    timestamp = DateTime.Now,
                    routes = new
                    {
                        login = "/api/v1/auth/login",
                        refresh = "/api/v1/auth/refresh",
                        logout = "/api/v1/auth/logout",
                        profile = "/api/v1/auth/profile",
                        validate = "/api/v1/auth/validate",
                    },
                }
            );
        }

        private static string GetUserIdFromAccessToken(string jwt)
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var subId = token
                .Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
                ?.Value;
            if (!string.IsNullOrEmpty(subId))
                return subId;

            return token.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value ?? token
                    .Claims.FirstOrDefault(c => c.Type == "sub")
                    ?.Value
                ?? string.Empty;
        }
    }
}
