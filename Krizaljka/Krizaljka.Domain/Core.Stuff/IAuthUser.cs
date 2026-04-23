namespace Krizaljka.Domain.Core.Stuff;

public interface IAuthUser
{
    public bool IsAuthenticatedAppUser { get; }
    public long Id { get;  }
}
