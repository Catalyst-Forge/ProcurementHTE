// Core/Models/RefreshToken.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

[Table("RefreshTokens")]
public class RefreshToken
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = default!;

    [Required, MaxLength(512)]
    public string Token { get; set; } = default!;

    [MaxLength(128)]
    public string? DeviceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
    public DateTime? RevokedAt { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }
}
