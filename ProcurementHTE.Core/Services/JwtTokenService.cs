using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly UserManager<User> _userManager;

        public JwtTokenService(IOptions<JwtSettings> jwtSettings, UserManager<User> userManager)
        {
            _jwtSettings = jwtSettings.Value;
            _userManager = userManager;
        }

        public async Task<string> GenerateTokenAsync(User user)
        {
            try
            {
                // 1. Ambil roles user
                var roles = await _userManager.GetRolesAsync(user);

                // 2. Buat claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName ?? user.UserName!),
                    new Claim(ClaimTypes.Email, user.Email!),
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                    new Claim(
                        JwtRegisteredClaimNames.Iat,
                        DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64
                    ),
                };

                // 3. Tambahkan roles sebagai claims
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // 4. Buat signing key
                var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.Secret);
                var key = new SymmetricSecurityKey(keyBytes);
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // 5. Set waktu expired
                var expires = DateTime.Now.AddMinutes(_jwtSettings.ExpirationInMinutes);

                // 6. Buat token descriptor
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = expires,
                    SigningCredentials = credentials,
                    Issuer = _jwtSettings.Issuer,
                    Audience = _jwtSettings.Audience,
                };

                // 7. Generate token
                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return tokenString;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
