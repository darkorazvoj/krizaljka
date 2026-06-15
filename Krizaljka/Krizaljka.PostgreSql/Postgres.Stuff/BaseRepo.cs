using Dapper;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.PostgreSql.Pagination;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Npgsql;

namespace Krizaljka.PostgreSql.Postgres.Stuff;

public abstract class BaseRepo<TDbKey>(IDbSession<TDbKey> dbSession)
{
    protected async Task<NpgsqlConnection?> GetConnectionAsync(TDbKey connKey, CancellationToken ct) =>
        (NpgsqlConnection?)await dbSession.OpenConnectionAsync(connKey, ct);

    protected async Task<T?> BaseExecuteWithOutAsync<T>(
        string sql,
        SqlParams parameters,
        string outParamName,
        TDbKey connKey,
        CancellationToken ct)
    {
        var conn = await GetConnectionAsync(connKey, ct);

        if (conn is null)
        {
            return default;
        }

        try
        {
            await conn.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    parameters,
                    transaction: dbSession.Transaction,
                    cancellationToken: ct
                ));
            return parameters.GetOutput<T?>(outParamName);
        }
        finally
        {
            await DisposeConnectionIfNeeded(conn);
        }
    }


    protected async Task<TCoreModel?> BaseGetAsync<TCoreModel, TDao>(
        string sql,
        SqlParams parameters,
        TDbKey connKey,
        CancellationToken ct)
        where TDao : IDao
    {
        var conn = await GetConnectionAsync(connKey, ct);

        if (conn is null)
        {
            return default;
        }

        try
        {
            var dao =
                await conn.QuerySingleOrDefaultAsync<TDao>(
                    new CommandDefinition(
                        sql, parameters,
                        transaction: dbSession.Transaction,
                        cancellationToken: ct));
            
            return dao is null ? default : dao.MapTo<TCoreModel>();
        }
        finally
        {
            await DisposeConnectionIfNeeded(conn);
        }
    }

    protected async Task<List<TCoreModel>> BaseGetListAsync<TCoreModel, TDao>(
        string sql,
        SqlParams parameters,
        TDbKey connKey,
        CancellationToken ct)
        where TDao : IDao
    {
        var conn = await GetConnectionAsync(connKey, ct);

        if (conn is null)
        {
            return [];
        }

        try
        {
            var listDao =
                (await conn.QueryAsync<TDao>(new CommandDefinition(
                    sql,
                    parameters,
                    transaction: dbSession.Transaction,
                    cancellationToken: ct)))
                .ToList();

            var list = listDao.Select(x => x.MapTo<TCoreModel>())
                .ToList();

            return list;
        }
        finally
        {
            await DisposeConnectionIfNeeded(conn);
        }
    }

    protected async Task BaseExecuteAsync(
        string sql,
        SqlParams? parameters,
        TDbKey connKey,
        CancellationToken ct)
    {
        var conn = await GetConnectionAsync(connKey, ct);

        if (conn is null)
        {
            return;
        }

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                sql,
                parameters,
                transaction: dbSession.Transaction,
                cancellationToken: ct));
        }
        finally
        {
            await DisposeConnectionIfNeeded(conn);
        }
    }

    internal async Task<PaginatedResult<List<TCoreModel>>> BaseGetPaginatedListAsync<TCoreModel, TDao>(
        IPaginationCore paginationCore,
        string viewName,
        Func<DaoPaginationParameters<TDao>> getDaoPaginationParameters,
        string? fixedWhereCondition,
        TDbKey connKey,
        CancellationToken ct)
        where TDao : IDao
    {
        var daoPaginationParameters = getDaoPaginationParameters();
        var paginationParameters = PaginationUtils.GetPaginationParameters(
            paginationCore,
            daoPaginationParameters);

        var conn = await GetConnectionAsync(connKey, ct);

        if (conn is null)
        {
            return new PaginatedResult<List<TCoreModel>>([], null);
        }

        try
        {
            var listDao = (await conn.QueryAsync<TDao>(new CommandDefinition(
                    PaginationOffsetUtils.GetSqlQuery(typeof(TDao), viewName, paginationParameters,
                        fixedWhereCondition),
                    paginationParameters.DynamicParameters,
                    transaction: dbSession.Transaction,
                    cancellationToken: ct)))
                .ToList();

            long? total = null;
            if (paginationParameters.GetTotal)
            {
                total = await conn.ExecuteScalarAsync<long>(
                    new CommandDefinition(
                        PaginationOffsetUtils.GetSqlQueryForTotal(viewName, paginationParameters, fixedWhereCondition),
                        paginationParameters.DynamicParameters,
                        transaction: dbSession.Transaction,
                        cancellationToken: ct));
            }

            var list = listDao.Select(x => x.MapTo<TCoreModel>())
                .ToList();

            return new PaginatedResult<List<TCoreModel>>(list, total);
        }
        finally
        {
            await DisposeConnectionIfNeeded(conn);

        }
    }

    private async Task DisposeConnectionIfNeeded(NpgsqlConnection conn)
    {
        if (!dbSession.HasTransaction)
        {
            await conn.DisposeAsync();
        }
    }
}
