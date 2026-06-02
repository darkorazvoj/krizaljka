using System.Security.Claims;

namespace Krizaljka.WebApi.Auth;

public static class GetAuthUserIdFromRequestService
{
    public static long? Invoke(ClaimsPrincipal user)
    {
        var idString = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(idString))
        {
            return null;
        }

        if (long.TryParse(idString, out var id))
        {
            return id;
        }

        return null;
    }
}
