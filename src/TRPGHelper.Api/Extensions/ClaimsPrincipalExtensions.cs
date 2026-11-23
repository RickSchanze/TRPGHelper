using System.Security.Claims;

namespace TRPGHelper.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return Guid.TryParse(raw, out var id)
            ? id
            : throw new InvalidOperationException("当前请求没有可用的用户身份。");
    }
}
