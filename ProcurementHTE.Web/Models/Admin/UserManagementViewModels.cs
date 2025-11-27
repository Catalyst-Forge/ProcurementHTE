using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Web.Models.Admin
{
    public class UserFiltersViewModel
    {
        public string? Search { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
        public string? TwoFactor { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class RoleOptionViewModel
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class UserListItemViewModel
    {
        public string Id { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string JobTitle { get; set; } = string.Empty;
        public string[] Roles { get; set; } = Array.Empty<string>();
        public bool EmailConfirmed { get; set; }
        public bool PhoneConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class UserManagementIndexViewModel
    {
        public UserFiltersViewModel Filters { get; set; } = new();
        public IReadOnlyList<UserListItemViewModel> Users { get; set; } =
            Array.Empty<UserListItemViewModel>();
        public IReadOnlyList<RoleOptionViewModel> AvailableRoles { get; set; } =
            Array.Empty<RoleOptionViewModel>();
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int TwoFactorEnabledCount { get; set; }
    }

    public class UserFormInputModel
    {
        public string? Id { get; set; }

        [Required]
        [Display(Name = "Nama Depan")]
        [StringLength(100)]
        public string FirstName { get; set; } = null!;

        [Display(Name = "Nama Belakang")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required]
        [Display(Name = "Username")]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = null!;

        [Display(Name = "Jabatan")]
        [StringLength(200)]
        public string? JobTitle { get; set; }

        [Display(Name = "Nomor HP (+62)")]
        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Role")]
        public IList<string> SelectedRoles { get; set; } = new List<string>();
    }

    public class UserFormPageViewModel
    {
        public UserFormInputModel Form { get; set; } = new();
        public IReadOnlyList<RoleOptionViewModel> Roles { get; set; } =
            Array.Empty<RoleOptionViewModel>();
        public bool IsEdit => !string.IsNullOrWhiteSpace(Form.Id);
        public string? GeneratedPassword { get; set; }
    }
}
