using Dapper;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.PostgreSql.Pagination;
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

    protected async Task<TCoreModel?> BaseGetAsync<TCoreModel, TDao>(
        string sql,
        SqlParams parameters,
        TDbKey connKey,
        CancellationToken cancellationToken)
        where TDao : IDao
    {
        await using var conn = await GetOpenedConnectionAsync(connKey, cancellationToken);

        var dao =
            await conn.QuerySingleOrDefaultAsync<TDao>(sql, parameters);

        return dao is null ? default : dao.MapTo<TCoreModel>();
    }

    protected async Task BaseExecuteAsync(
        string sql, 
        SqlParams? parameters,
        TDbKey connKey,
        CancellationToken cancellationToken)
    {
        await using var conn = await GetOpenedConnectionAsync(connKey, cancellationToken);
        await conn.ExecuteAsync(sql, parameters);
    }

    internal async Task<PaginatedResult<List<TCoreModel>>> BaseGetPaginatedListAsync<TCoreModel, TDao>(
        IPaginationCore paginationCore,
        string viewName,
        Func<DaoPaginationParameters<TDao>> getDaoPaginationParameters,
        CancellationToken cancellationToken)
    {
        var daoPaginationParameters = getDaoPaginationParameters();
        var paginationParameters = PaginationUtils.GetPaginationParameters(
            paginationCore,
            daoPaginationParameters);

        return new PaginatedResult<List<TCoreModel>>(new InvalidPagination(), [], 0, false);

    }
}
