using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.ViewModels;
using System.Security.Claims;

namespace ProcurementHTE.Web.Controllers {
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
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null!) {
      ViewData["ReturnUrl"] = returnUrl;

      if (ModelState.IsValid) {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user != null && user.IsActive) {
          var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            isPersistent: true,
            lockoutOnFailure: true
          );

          if (result.Succeeded) {
            user.LastLoginAt = DateTime.Now;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("User {model.Email} logged in", model.Email);

            var claims = new List<Claim> {
              new(ClaimTypes.Name, user.FullName!),
              new(ClaimTypes.Email, user.Email!),
              new(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Login");
            var authProperties = new AuthenticationProperties {
              IsPersistent = true
            };

            await HttpContext.SignInAsync(
              IdentityConstants.ApplicationScheme, 
              new ClaimsPrincipal(claimsIdentity), 
              authProperties);

            var existingClaims = await _userManager.GetClaimsAsync(user);

            if (!existingClaims.Any(c => c.Type == ClaimTypes.Name)) {
              await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, user.FullName!));
            }

            if (!existingClaims.Any(c => c.Type == ClaimTypes.Email)) {
              await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, user.Email!));
            }

            if (!existingClaims.Any(c => c.Type == ClaimTypes.NameIdentifier)) {
              await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            }

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
    public async Task<IActionResult> Logout() {
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
