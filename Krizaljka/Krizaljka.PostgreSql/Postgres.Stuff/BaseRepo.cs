using Dapper;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Npgsql;

namespace Krizaljka.PostgreSql.Postgres.Stuff;

public abstract class BaseRepo<TDbKey>(IReadOnlyDictionary<TDbKey, string> connections)
{
    public NpgsqlConnection GetOpenedConnection(TDbKey connKey)
    {
        var conn = new NpgsqlConnection(GetConnectionString(connKey));
        conn.Open();
        return conn;
    }

    protected async Task<NpgsqlConnection> GetOpenedConnectionAsync(
        TDbKey connKey,
        CancellationToken ct)
    {
        var conn = new NpgsqlConnection(GetConnectionString(connKey));
        await conn.OpenAsync(ct);
        return conn;
    }

    private string GetConnectionString(TDbKey connKey)
    {
        if (!connections.TryGetValue(connKey, out var connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string registered for connKey '{connKey}'");
        }

        return connectionString;
    }

    protected async Task<T?> BaseExecuteWithOutAsync<T>(
        string sql, 
        SqlParams parameters,
        string outParamName,
        TDbKey connKey,
        CancellationToken cancellationToken)
    {
        await using var conn = await GetOpenedConnectionAsync(connKey, cancellationToken);

        await conn.ExecuteAsync(sql, parameters);
        return parameters.GetOutput<T?>(outParamName);
    }
}
