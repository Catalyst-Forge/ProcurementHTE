using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using project_25_07.Models;
using project_25_07.Models.ViewModels;

namespace project_25_07.Controllers {
  public class AuthController(
    UserManager<User> userManager, 
    SignInManager<User> signInManager, 
    RoleManager<Role> roleManager, ILogger<AuthController> logger
    ) : Controller {
    private readonly UserManager<User> _userManager = userManager;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly RoleManager<Role> _roleManager = roleManager;
    private readonly ILogger<AuthController> _logger = logger;


    /*
     * GET: Register
     */
    public IActionResult Register() {
      var model = new RegisterViewModel();
      ViewBag.Roles = _roleManager.Roles.ToList();
      return View(model);
    }

    /*
     * POST: Register
     */
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model) {
      ViewBag.Roles = _roleManager.Roles.ToList();

      if (ModelState.IsValid) {
        var user = new User {
          UserName = model.UserName,
          Email = model.Email,
          FirstName = model.FirstName,
          LastName = model.LastName,
          EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded) {
          await _userManager.AddToRoleAsync(user, model.SelectedRole);
          string message = $"User {user.Email} created with role {model.SelectedRole}";
          _logger.LogInformation(message);
          await _signInManager.SignInAsync(user, isPersistent: false);

          return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors) {
          ModelState.AddModelError(string.Empty, error.Description);
        }
      }

      return View(model);
    }

    /*
     * GET: Login
     */
    public IActionResult Login(string? returnUrl = null) {
      ViewData["ReturnUrl"] = returnUrl;
      return View();
    }

    /*
     * POST: Login
     */
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null) {
      ViewData["ReturnUrl"] = returnUrl;

      if (ModelState.IsValid) {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user != null && user.IsActive) {
          var result = await _signInManager.PasswordSignInAsync(
            user.UserName,
            model.Password,
            isPersistent: true,
            lockoutOnFailure: true
          );

          if (result.Succeeded) {
            _logger.LogInformation("User {model.Email} logged in", model.Email);

            return RedirectToLocal(returnUrl);
          }

          if (result.IsLockedOut) {
            ModelState.AddModelError(string.Empty, "Akun Anda terkunci. Coba lagi nanti.");
          } else {
            ModelState.AddModelError(string.Empty, "Email atau Password salah");
          }
        } else {
          ModelState.AddModelError(string.Empty, "Akun tidak ditemukan atau tidak aktif");
        }
      }

      return View(model);
    }

    /*
     * POST: Logout
     */
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> logout() {
      await _signInManager.SignOutAsync();
      _logger.LogInformation("User logged out");

      return RedirectToAction("Index", "Home");
    }

    /*
     * GET: Access Denied
     */
    public IActionResult AccessDenied() {
      return View();
    }

    private IActionResult RedirectToLocal(string returnUrl) {
      if (Url.IsLocalUrl(returnUrl)) {
        return Redirect(returnUrl);
      } else {
        return RedirectToAction("Index", "Home");
      }
    }
  }
}
