using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.PostgreSql.Postgres.Stuff.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;

namespace Krizaljka.PostgreSql.Postgres.Stuff;

public sealed class DbSession(ConnectionStrings connectionStrings, ILogger<DbSession> logger) : IDbSession<ConnStrings>
{
    public  NpgsqlConnection? TransactionConnection { get; private set; }

    public DbTransaction? Transaction { get; private set; }

    public bool HasTransaction => TransactionConnection is not null && Transaction is not null;

    public async Task<IServiceResult> ExecuteInTransactionAsync(
        Func<CancellationToken, Task<IServiceResult>> action,
        ConnStrings connKey,
        CancellationToken ct)
    {

        if (HasTransaction)
        {
            return await action(ct);
        }

        if (!await BeginTransactionAsync(connKey, ct))
        {
            return new DatabaseOperationFailed();
        }

        try
        {
            var result = await action(ct);

            if (!ShouldCommitTransaction.ForResult(result))
            {
                await RollbackAsync(ct);
                return result;
            }

            if (!await CommitAsync(ct))
            {
                return new DatabaseOperationFailed();
            }
            
            return result;
        }
        catch (Exception e) 
        {
            await RollbackAsync(ct);
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "DB transaction rollback.");
            }
            return new DatabaseOperationFailed();
        }
    }

    public async Task<DbConnection?> OpenConnectionAsync(ConnStrings connKey, CancellationToken ct)
    {
        if (TransactionConnection is not null)
        {
            return TransactionConnection;
        }

        try
        {
            var connection = new NpgsqlConnection(connectionStrings.GetConnectionString(connKey));
            await connection.OpenAsync(ct);

            return connection;
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "NON-transaction connection open FAILED!");
            }

            return null;
        }
    }

    public async Task<bool> BeginTransactionAsync(ConnStrings connKey, CancellationToken ct)
    {
        if (HasTransaction)
        {
            return false;
        }

        try
        {
            TransactionConnection = new NpgsqlConnection(connectionStrings.GetConnectionString(connKey));
            await TransactionConnection.OpenAsync(ct);

            Transaction = await TransactionConnection.BeginTransactionAsync(ct);

            return true;
        }
        catch (Exception e)
        {
            await DisposeTransactionAsync();

            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Starting transaction failed!");
            }

            return false;
        }
    }

    public async Task<bool> CommitAsync(CancellationToken ct)
    {
        if (Transaction is null)
        {
            return false;
        }

        try
        {
            await Transaction.CommitAsync(ct);
            await DisposeTransactionAsync();

            return true;
        }
        catch (Exception e)
        {
           
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "COMMIT FAILED.");
            }

            await DisposeTransactionAsync();
            return false;
        }
    }

    private async Task RollbackAsync(CancellationToken ct)
    {
        try
        {
            if (Transaction is not null)
            {
                await Transaction.RollbackAsync(ct);
            }

            await DisposeTransactionAsync();
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "ROLLBACK FAILED.");
            }

            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        try
        {
            if (Transaction is not null)
            {
                await Transaction.DisposeAsync();
                Transaction = null;
            }

            if (TransactionConnection is not null)
            {
                await TransactionConnection.DisposeAsync();
                TransactionConnection = null;
            }
        }
        catch
        {
            Transaction = null;
            TransactionConnection = null;

            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Dispose transaction call(s) failed. Transaction and connection set to NULL.");
            }
        }
    }

    public async ValueTask DisposeAsync() => await DisposeTransactionAsync();
}
