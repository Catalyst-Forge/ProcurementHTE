using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    private bool IsCooldownActive(string key, out int remainingSeconds)
    {
        remainingSeconds = CooldownHelper.GetRemainingSeconds(HttpContext.Session, key);
        return remainingSeconds > 0;
    }

    private void StartCooldown(string key) =>
        CooldownHelper.SetCooldown(
            HttpContext.Session,
            key,
            TimeSpan.FromSeconds(CodeCooldownSeconds)
        );

    private int GetCooldownSeconds(string key) =>
        CooldownHelper.GetRemainingSeconds(HttpContext.Session, key);

    private JsonResult CooldownJsonResult(int remaining) =>
        Json(
            new
            {
                success = false,
                message = $"Tunggu {remaining} detik sebelum mengirim ulang.",
                remaining,
            }
        );
}
