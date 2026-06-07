using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    private IActionResult AjaxModelError(string? fallbackMessage = null)
    {
        var errors = ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .Select(x => new
            {
                field = x.Key,
                messages = x.Value!.Errors.Select(e => e.ErrorMessage).ToArray(),
            })
            .ToArray();

        return BadRequest(
            new
            {
                success = false,
                message = fallbackMessage ?? "Permintaan tidak valid.",
                errors,
            }
        );
    }

    private bool IsAjaxRequest()
    {
        var headers = Request?.Headers;
        if (headers is null)
            return false;

        if (
            headers.TryGetValue("X-Requested-With", out var requestedWith)
            && requestedWith == "XMLHttpRequest"
        )
            return true;

        foreach (var accept in headers["Accept"])
        {
            if (
                !string.IsNullOrEmpty(accept)
                && accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            )
                return true;
        }

        return false;
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return Redirect("~/");
    }
}
