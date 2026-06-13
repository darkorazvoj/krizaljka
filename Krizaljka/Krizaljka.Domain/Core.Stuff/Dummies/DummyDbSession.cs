using System.Data.Common;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.Domain.Core.Stuff.Dummies;

internal class DummyDbSession<TDbKey> : IDbSession<TDbKey>
{
    public DbTransaction? Transaction => null;

    public bool HasTransaction => false;

    public async Task<IServiceResult> ExecuteInTransactionAsync(
        Func<CancellationToken, Task<IServiceResult>> action,
        TDbKey connKey,
        CancellationToken ct)
    {
        try
        {
            return await action(ct);
        }
        catch
        {
            return new DatabaseOperationFailed();
        }
    }

    public Task<DbConnection?> OpenConnectionAsync(TDbKey connKey, CancellationToken ct) =>
        Task.FromResult<DbConnection?>(null);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
