using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace project_25_07.Models {
  public class Role : IdentityRole {
    [StringLength(200)]
    public string Description { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
  }
}
