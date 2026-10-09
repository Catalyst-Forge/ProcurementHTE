using System.Security.Claims;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Web.Utils;

public sealed class HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
