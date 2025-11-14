using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

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

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public string? FullName { get; private set; }

        public ICollection<Procurement> Procurements { get; set; } = [];
    }
}
