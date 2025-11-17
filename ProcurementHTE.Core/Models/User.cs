using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Models.Enums;

namespace ProcurementHTE.Core.Models
{
    public class User : IdentityUser
    {
        [Required(ErrorMessage = "Username wajib diisi")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Username harus antara 3-30 karakter")]
        public override string? UserName { get; set; } = null!;

        [Required(ErrorMessage = "Nama depan wajib diisi")]
        [StringLength(100)]
        public string FirstName { get; set; } = null!;

        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? JobTitle { get; set; }

        [StringLength(256)]
        public string? AvatarObjectKey { get; set; }

        [StringLength(200)]
        public string? AvatarFileName { get; set; }

        public DateTime? AvatarUpdatedAt { get; set; }

        public DateTime? PasswordChangedAt { get; set; }

        public TwoFactorMethod TwoFactorMethod { get; set; } = TwoFactorMethod.None;

        public string? RecoveryCodesJson { get; set; }

        public bool RecoveryCodesHidden { get; set; }

        public DateTime? RecoveryCodesGeneratedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public string? FullName { get; private set; }

        public ICollection<UserSession> Sessions { get; set; } = [];

        public ICollection<UserSecurityLog> SecurityLogs { get; set; } = [];

        public ICollection<Procurement> Procurements { get; set; } = [];
    }
}
