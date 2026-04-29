
using Krizaljka.Domain.Template;
using Krizaljka.PostgreSql.Postgres.Stuff;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using System.Data;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.PostgreSql.Sql;

namespace Krizaljka.PostgreSql.KrizaljkaTemplatePostgreSql;

internal class KrizaljkaTemplateRepo(IReadOnlyDictionary<ConnStrings, string> conns)
    : BaseRepo<ConnStrings>(conns), IKrizaljkaTemplateRepo
{
    public async Task<long> InsertAsync(
        int[][] matrix,
        string? name,
        int numOfRows,
        int numOfColumns,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct) =>
        await BaseExecuteWithOutAsync<long>(
            $"call {Procs.TemplateInsert} (@name, @matrix, @numRows, @numColumns, @isActive, @createdOn, @RanById, null);",
            new SqlParams()
                .Add("name", name)
                .AddJsonb("matrix", matrix)
                .Add("numRows", numOfRows)
                .Add("numColumns", numOfColumns)
                .Add("isactive", true)
                .Add("createdOn", createdOn)
                .Add("ranById", ranById)
                .AddOutput("newId", DbType.Int64),
            "newId",
            ConnStrings.Core,
            ct);

    public Task<KrizaljkaTemplate?> GetAsync(long id, CancellationToken ct)=>
        BaseGetAsync<KrizaljkaTemplate, KrizaljkaTemplateDao>(
            $"select {DaoUtils.GetSelectColumns(typeof(KrizaljkaTemplateDao))} from {Procs.TemplateView} where id = @id",
            new SqlParams()
                .Add("id", id),
            ConnStrings.Core,
            ct);

    public Task<PaginatedResult<List<KrizaljkaTemplateListItem>>> GetListAsync(IPaginationCore paginationCore,
        CancellationToken ct) =>
        BaseGetPaginatedListAsync<KrizaljkaTemplateListItem, KrizaljkaTemplateListItemDao>(
            paginationCore,
            Procs.TemplateView,
            KrizaljkaTemplateListItemDao.ToDaoPaginationParameters,
            ConnStrings.Core,
            ct);
}
