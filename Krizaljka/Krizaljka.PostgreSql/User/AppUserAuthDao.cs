using Krizaljka.Domain.User.Models;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;


namespace Krizaljka.PostgreSql.User;

internal record AppUserAuthDao(
    long Id,
    string LoginEmail,
    string? PasswordHash,
    bool EmailVerified,
    int LoginFailedAttempt,
    bool IsBlocked,
    DateTimeOffset? BlockedUntil,
    bool IsActive,
    string Changestamp): IDao
{
    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(AppUserAuth))
        {
            object result = new AppUserAuth(
                Id,
                LoginEmail,
                PasswordHash,
                EmailVerified,
                LoginFailedAttempt,
                IsBlocked,
                BlockedUntil,
                IsActive,
                Changestamp);
            return (TCoreModel)result;
        }
        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
