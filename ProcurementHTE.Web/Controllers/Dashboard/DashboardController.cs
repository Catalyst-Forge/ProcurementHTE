using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Authorization;

namespace ProcurementHTE.Web.Controllers.Dashboard
{
    [Authorize]
    [Route("Dashboard")]
    public class DashboardController : Controller
    {
        private readonly UserManager<User> _userManager;

        public DashboardController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("")]
        [HttpGet("/")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                if (DashboardRoleHelper.TryGetControllerForRole(role, out var controller))
                {
                    return RedirectToAction("Index", controller);
                }
            }

            return View("~/Views/Dashboard/UnknownRole.cshtml");
        }

        [HttpGet("UnknownRole")]
        public IActionResult UnknownRole()
        {
            return View("~/Views/Dashboard/UnknownRole.cshtml");
        }
    }
}
