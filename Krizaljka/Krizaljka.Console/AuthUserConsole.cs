using Krizaljka.Domain.Core.Stuff;

namespace Krizaljka.Console;

public class AuthUserConsole: IAuthUser
{
    public bool IsAuthenticatedAppUser { get; } = true;
    public long Id { get; } = 7;
}
