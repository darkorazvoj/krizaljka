using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.PostgreSql.Postgres.Stuff;
using System.Data;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.TermDescription;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.PostgreSql.Sql;

namespace Krizaljka.PostgreSql.TermDescriptionPostgreSql;

internal class TermDescriptionRepo(IDbSession<ConnStrings> dbSession)
    : BaseRepo<ConnStrings>(dbSession), ITermDescriptionRepo
{
    public Task<long> InsertDescriptionAsync(
        long termId,
        string description,
        long? batchId,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct) => BaseExecuteWithOutAsync<long>(
        $"call {Procs.TermDescriptionInsert} (@termId, @description, @batchId, @createdOn, @RanById, null);",
        new SqlParams()
            .Add("termId", termId)
            .Add("description", description)
            .Add("batchId", batchId)
            .Add("createdOn", createdOn)
            .Add("ranById", ranById)
            .AddOutput("newId", DbType.Int64),
        "newId",
        ConnStrings.Core,
        ct);

    public Task<TermDescription?> GetAsync(long id, CancellationToken ct) =>
        BaseGetAsync<TermDescription, TermDescriptionDao>(
            $"select {DaoUtils.GetSelectColumns(typeof(TermDescriptionDao))} from {Procs.TermDescriptionView} where id = @id",
            new SqlParams()
                .Add("id", id),
            ConnStrings.Core,
            ct);

    public Task<PaginatedResult<List<TermDescriptionListItem>>> GetListAsync(
        IPaginationCore paginationCore,
        CancellationToken ct) =>
        BaseGetPaginatedListAsync<TermDescriptionListItem, TermDescriptionListItemDao>(
            paginationCore,
            Procs.TermDescriptionView,
            TermDescriptionListItemDao.ToDaoPaginationParameters,
            null,
            ConnStrings.Core,
            ct);

    public Task<string?>
        UpdateAsync(long id, string description, string changestamp, CancellationToken ct) =>
        BaseExecuteWithOutAsync<string?>(
            $"call {Procs.TermDescriptionUpdate}(@id, @description, @changestamp,  null);",
            new SqlParams()
                .Add("id", id)
                .Add("description", description)
                .Add("changestamp", changestamp)
                .AddOutput("newchangestamp", dbType: DbType.String),
            "newchangestamp",
            ConnStrings.Core,
            ct);

    public Task DeleteAsync(
        long id,
        string changestamp,
        CancellationToken ct) =>
        BaseExecuteAsync(
            $"call {Procs.TermDescriptionDelete}(@id, @changestamp);",
            new SqlParams()
                .Add("id", id)
                .Add("changestamp", changestamp),
            ConnStrings.Core,
            ct);
}
