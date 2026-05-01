using Krizaljka.Domain.User.Models;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.User;

internal record AppUserMeDao(
    long Id,
    string LoginEmail,
    string? FirstName,
    string? LastName,
    string? DisplayName) : IDao
{
    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(AppUserMe))
        {
            object result = new AppUserMe(
                Id,
                LoginEmail,
                FirstName,
                LastName,
                DisplayName);
            return (TCoreModel)result;
        }
        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");    }
}
