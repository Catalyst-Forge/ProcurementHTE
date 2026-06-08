using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.ApiController
{
    [ApiController]
    [Area("Api")]
    [Route("api/v1/auth")]
    public partial class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly JwtSettings _jwtSettings;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public AuthController(
            IAuthService authService,
            IOptions<JwtSettings> jwtSettings,
            IRefreshTokenRepository refreshTokenRepository
        )
        {
            _authService = authService;
            _jwtSettings = jwtSettings.Value;
            _refreshTokenRepository = refreshTokenRepository;
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
                    return Unauthorized(new { message = "Token is invalid." });
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
            catch (Exception)
            {
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
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
