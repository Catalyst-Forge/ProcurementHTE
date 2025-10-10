using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers {
  public class DashboardController : Controller {
    public IActionResult Index() {
      return View();
    }
  }
}
