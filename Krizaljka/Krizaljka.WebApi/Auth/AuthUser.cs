using System.Security.Claims;
using Krizaljka.Domain.Core.Stuff;

namespace Krizaljka.WebApi.Auth;

internal class AuthUser: IAuthUser
{
    public bool IsAuthenticatedAppUser { get; }
    public long Id { get; }

    public AuthUser(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        IsAuthenticatedAppUser = user?.Identity?.IsAuthenticated == true;

        if (!IsAuthenticatedAppUser)
        {
            return;
        }

        var idValue =
            user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue("sub");

        if (long.TryParse(idValue, out var id))
        {
            Id = id;
        }

    }
}
