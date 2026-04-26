using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.Domain.User;

internal static class UserStatusValidation
{
    public static IServiceResult Get(
        bool isActive,
        bool isEmailVerified,
        bool isBlocked,
        DateTimeOffset? blockedUntil,
        DateTimeOffset utcNow)
    {
        if (!isActive)
        {
            return new InvalidCredentials();
        }

        if (!isEmailVerified)
        {
            return new EmailNotVerified();
        }

        if (isBlocked)
        {
            if (blockedUntil is not null &&
                blockedUntil <= utcNow)
            {
                return new ShouldUnblockUser();
            }

            return new InvalidCredentials();
        }

        return new Success();
    }
}
