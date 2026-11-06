using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using ProcurementHTE.Web.Extensions;

namespace ProcurementHTE.Tests.Auth;

public class LoginRedirectTests
{
    private const string LoginUri = "https://app.test/Auth/Login?ReturnUrl=%2FPurchaseRequisitions";

    [Fact]
    public async Task Expired_session_on_htmx_request_returns_401_without_redirect()
    {
        var response = await ChallengeAsync("/PurchaseRequisitions", htmx: true);

        Assert.Equal(StatusCodes.Status401Unauthorized, response.StatusCode);
        Assert.False(response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task Expired_session_on_full_page_request_redirects_to_login()
    {
        var response = await ChallengeAsync("/PurchaseRequisitions", htmx: false);

        Assert.Equal(StatusCodes.Status302Found, response.StatusCode);
        Assert.Equal(LoginUri, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Expired_session_on_api_request_returns_401()
    {
        var response = await ChallengeAsync("/api/vendors", htmx: false);

        Assert.Equal(StatusCodes.Status401Unauthorized, response.StatusCode);
    }

    private static async Task<HttpResponse> ChallengeAsync(string path, bool htmx)
    {
        var options = new CookieAuthenticationOptions();
        IdentityServiceExtensions.ConfigureApplicationCookie(options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        if (htmx)
        {
            httpContext.Request.Headers["HX-Request"] = "true";
        }

        var scheme = new AuthenticationScheme(
            CookieAuthenticationDefaults.AuthenticationScheme,
            null,
            typeof(CookieAuthenticationHandler)
        );
        var context = new RedirectContext<CookieAuthenticationOptions>(
            httpContext,
            scheme,
            options,
            new AuthenticationProperties(),
            LoginUri
        );

        await options.Events.OnRedirectToLogin(context);
        return httpContext.Response;
    }
}
