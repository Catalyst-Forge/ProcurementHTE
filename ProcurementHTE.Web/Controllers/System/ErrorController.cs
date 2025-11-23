using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Models;

namespace ProcurementHTE.Web.Controllers.SystemModule;

[AllowAnonymous]
[Route("Error")]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("")]
    [HttpGet("500")]
    public IActionResult ServerError()
    {
        ConfigureChromeLessLayout();

        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        if (exceptionFeature?.Error is not null)
        {
            _logger.LogError(
                exceptionFeature.Error,
                "Unhandled exception at path {Path}",
                exceptionFeature.Path ?? HttpContext.Request.Path
            );
        }

        Response.StatusCode = StatusCodes.Status500InternalServerError;

        var viewModel = BuildViewModel(
            StatusCodes.Status500InternalServerError,
            "Kami sedang mengalami gangguan",
            "Sistem kami mengalami kendala saat memproses permintaan Anda. Kami sudah mencatat kejadian ini dan akan segera menanganinya.",
            exceptionFeature?.Path ?? HttpContext.Request.Path
        );

        ApplyAnonymousFallback(viewModel);

        return View("~/Views/Shared/Error.cshtml", viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("{statusCode:int:min(400)}", Order = 1)]
    public IActionResult Status(int statusCode)
    {
        ConfigureChromeLessLayout(ShouldHideChrome(statusCode));

        var feature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        var originalPath = feature?.OriginalPath ?? HttpContext.Request.Path;

        var (title, description, primaryText, primaryUrl, secondaryText, secondaryUrl) = statusCode switch
        {
            StatusCodes.Status404NotFound => (
                "Halaman tidak ditemukan",
                "Kami tidak menemukan halaman yang Anda minta atau mungkin sudah dipindahkan.",
                "Kembali ke Dashboard",
                Url.Action("Index", "Dashboard") ?? "/",
                null,
                null
            ),
            StatusCodes.Status403Forbidden => (
                "Akses dibatasi",
                "Anda tidak memiliki izin untuk membuka halaman ini. Jika menurut Anda ini suatu kesalahan, hubungi administrator.",
                "Kembali ke Dashboard",
                Url.Action("Index", "Dashboard") ?? "/",
                null,
                null
            ),
            StatusCodes.Status401Unauthorized => (
                "Sesi Anda berakhir",
                "Silakan masuk kembali agar kami dapat memverifikasi identitas Anda sebelum melanjutkan.",
                "Masuk ke Akun",
                Url.Action("Login", "Auth") ?? "/Auth/Login",
                null,
                null
            ),
            _ when statusCode >= 500 => (
                "Layanan sedang bermasalah",
                "Terjadi kesalahan pada server kami. Tim sedang melakukan penanganan.",
                "Muat ulang halaman",
                originalPath,
                "Kembali ke Dashboard",
                Url.Action("Index", "Dashboard")
            ),
            _ => (
                "Terjadi kesalahan",
                "Permintaan Anda tidak dapat kami proses saat ini.",
                "Kembali ke Dashboard",
                Url.Action("Index", "Dashboard") ?? "/",
                null,
                null
            )
        };

        Response.StatusCode = statusCode;

        var viewModel = BuildViewModel(
            statusCode,
            title,
            description,
            originalPath,
            primaryText,
            primaryUrl,
            secondaryText,
            secondaryUrl
        );

        ApplyAnonymousFallback(viewModel);

        return View("~/Views/Shared/Error.cshtml", viewModel);
    }

    private void ConfigureChromeLessLayout(bool hideChrome = true)
    {
        ViewData["HideSidebar"] = hideChrome;
        ViewData["HideNavbar"] = hideChrome;
        ViewData["HideFooter"] = hideChrome;
        ViewData["DisableHxBoost"] = hideChrome;

        if (hideChrome)
        {
            ViewData["BodyClass"] = "bg-body-tertiary";
        }
        else if (ViewData.ContainsKey("BodyClass"))
        {
            ViewData.Remove("BodyClass");
        }
    }

    private void ApplyAnonymousFallback(ErrorViewModel model)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return;
        }

        model.PrimaryActionText = "Masuk ke Akun";
        model.PrimaryActionUrl = Url.Action("Login", "Auth") ?? "/Auth/Login";
        model.SecondaryActionText = null;
        model.SecondaryActionUrl = null;
    }

    private bool ShouldHideChrome(int statusCode)
    {
        if (User?.Identity?.IsAuthenticated == true && statusCode == StatusCodes.Status404NotFound)
        {
            return false;
        }

        return true;
    }

    private ErrorViewModel BuildViewModel(
        int statusCode,
        string title,
        string description,
        string? path,
        string? primaryText = null,
        string? primaryUrl = null,
        string? secondaryText = null,
        string? secondaryUrl = null)
    {
        return new ErrorViewModel
        {
            StatusCode = statusCode,
            Title = title,
            Description = description,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            RequestPath = path,
            PrimaryActionText = primaryText ?? "Kembali ke Dashboard",
            PrimaryActionUrl = primaryUrl ?? (Url.Action("Index", "Dashboard") ?? "/"),
            SecondaryActionText = secondaryText,
            SecondaryActionUrl = secondaryUrl
        };
    }
}
