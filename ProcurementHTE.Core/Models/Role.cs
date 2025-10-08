using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models {
  public class Role : IdentityRole {
    [StringLength(200)]
    public string Description { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
  }
}
