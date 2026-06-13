
using System.Data.Common;
using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.Domain.Core.Stuff.DatabaseStuff;

public interface IDbSession<in TDbKey>: IAsyncDisposable
{
    DbTransaction? Transaction { get; }

    bool HasTransaction { get; }

    Task<IServiceResult> ExecuteInTransactionAsync(
        Func<CancellationToken, Task<IServiceResult>> action,
        TDbKey connKey,
        CancellationToken ct);

    Task<DbConnection?> OpenConnectionAsync(TDbKey connKey, CancellationToken ct);
}
