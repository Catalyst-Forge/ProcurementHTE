using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.Api
{
    public partial class AuthController
    {
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
                        new LoginResponseDto { Success = false, Message = "Invalid data." }
                    );
                }

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var tokenResponse = await _authService.LoginAsync(model, ip, ct);

                var userId = GetUserIdFromAccessToken(tokenResponse.AccessToken);
                var profile = await _authService.GetProfileAsync(userId, ct);

                var expiresAt = DateTime.Now.AddMinutes(_jwtSettings.ExpirationInMinutes);

                return Ok(
                    new LoginResponseDto
                    {
                        Success = true,
                        Message = "Login successful.",
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
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(
                    new LoginResponseDto
                    {
                        Success = false,
                        Message = "Email/username or password is incorrect.",
                    }
                );
            }
            catch (Exception)
            {
                return StatusCode(
                    500,
                    new LoginResponseDto
                    {
                        Success = false,
                        Message = "An internal server error occurred.",
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
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { success = false, message = "Refresh token is invalid." });
            }
            catch (Exception)
            {
                return StatusCode(
                    500,
                    new { success = false, message = "An internal server error occurred." }
                );
            }
        }

    }
}
