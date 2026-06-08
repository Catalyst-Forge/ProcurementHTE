using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.ApiController
{
    public partial class AuthController
    {
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
                    return Unauthorized(new { success = false, message = "Token is invalid." });

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
            catch (Exception)
            {
                return StatusCode(
                    500,
                    new { success = false, message = "An internal server error occurred." }
                );
            }
        }
        
        [HttpGet("validate")]
        [Authorize]
        public async Task<IActionResult> ValidateToken(
            [FromQuery] string? deviceId,
            CancellationToken ct
        )
        {
            var userId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(
                    new
                    {
                        valid = false,
                        message = "Token is invalid.",
                        reason = "NoUserIdClaim",
                        timestamp = DateTime.Now,
                    }
                );
            }

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
                            message = "Token considered invalid because the device session has logged out.",
                            reason = "NoActiveRefreshTokenForDevice",
                            userId,
                            deviceId,
                            timestamp = DateTime.Now,
                        }
                    );
                }
            }

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
    }
}
