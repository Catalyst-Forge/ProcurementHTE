using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.Account;

[Authorize(Roles = "Admin")]
public partial class UserManagementController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<Role> _roleManager;

    public UserManagementController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<Role> roleManager
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }
}
