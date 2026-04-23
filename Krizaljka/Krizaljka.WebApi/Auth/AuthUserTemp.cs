using Krizaljka.Domain.Core.Stuff;

namespace Krizaljka.WebApi.Auth;

internal class AuthUserTemp: IAuthUser
{
    public bool IsAuthenticatedAppUser { get; } = true;
    public long Id { get; } = 7;
}
