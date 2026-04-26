using Krizaljka.Domain.User.Models;

namespace Krizaljka.Domain.User.Repo;

public interface IAppUserRepo
{
    Task<AppUserAuth?> GetByLoginEmailAsync(string username, CancellationToken ct);
    Task IncreaseLoginAttemptAsync(long id, string changestamp, long ranById, CancellationToken ct);
    Task IncreaseLoginAttemptAndBlockAsync(long id, DateTimeOffset blockedUntil, string changestamp, long ranById, CancellationToken ct);
    Task UnblockAsync(long id, string changestamp, long ranById, CancellationToken ct);
    Task ResetLoginAttemptsAsync(long id, string changestamp, long serviceUserId, CancellationToken ct);

}
