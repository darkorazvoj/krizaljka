
namespace Krizaljka.Domain.User.Models;

public record AppUserMin(
    long Id,
    string LoginEmail,
    bool EmailVerified,
    bool IsBlocked,
    DateTimeOffset? BlockedUntil,
    bool IsActive,
    string Changestamp);
