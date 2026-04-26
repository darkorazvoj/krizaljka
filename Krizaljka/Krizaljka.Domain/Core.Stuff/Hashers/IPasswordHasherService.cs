
using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.Domain.Core.Stuff.Hashers;

public interface IPasswordHasherService
{
    string HashPassword(string password);
    IServiceResult VerifyHashedPassword(string passwordHash, string password);

}
