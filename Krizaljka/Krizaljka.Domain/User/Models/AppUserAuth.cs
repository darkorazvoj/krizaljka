
namespace Krizaljka.Domain.User.Models;

public record AppUserAuth(
    long Id,
    string LoginEmail,
    string? PasswordHash,
    bool EmailVerified,
    int LoginFailedAttempt,
    bool IsBlocked,
    DateTimeOffset? BlockedUntil,
    bool IsActive,
    string Changestamp);