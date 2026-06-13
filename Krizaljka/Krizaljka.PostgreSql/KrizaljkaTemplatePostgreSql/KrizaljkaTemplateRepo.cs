
using Krizaljka.Domain.Template;
using Krizaljka.PostgreSql.Postgres.Stuff;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using System.Data;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.PostgreSql.Sql;

namespace Krizaljka.PostgreSql.KrizaljkaTemplatePostgreSql;

internal class KrizaljkaTemplateRepo(IDbSession<ConnStrings> dbSession)
    : BaseRepo<ConnStrings>(dbSession), IKrizaljkaTemplateRepo
{
    public async Task<long> InsertAsync(
        int[][] matrix,
        string matrixKey,
        string? name,
        int numOfRows,
        int numOfColumns,
        int numZeroBlocks,
        List<TemplateBlock> zeroBlocks,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct) =>
        await BaseExecuteWithOutAsync<long>(
            $"call {Procs.TemplateInsert} (@name, @matrix, @matrixKey, @numRows, @numColumns, @numZeroBlocks, @zeroBlocks, @isActive, @createdOn, @RanById, null);",
            new SqlParams()
                .Add("name", name)
                .AddMatrix("matrix", matrix)
                .Add("matrixKey", matrixKey)
                .Add("numRows", numOfRows)
                .Add("numColumns", numOfColumns)
                .Add("@numZeroBlocks", numZeroBlocks)
                .AddZeroBlocks("@zeroBlocks", zeroBlocks)
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

    public Task<List<KrizaljkaTemplateExport>> GetForExportAsync(List<long> ids, CancellationToken ct) =>
        BaseGetListAsync<KrizaljkaTemplateExport, KrizaljkaTemplateExportDao>(
            $"select {DaoUtils.GetSelectColumns(typeof(KrizaljkaTemplateExportDao))} from {Procs.TemplateView}  {(ids.Count > 0 ? "where id = ANY(@ids)" : "")};",
            new SqlParams()
                .Add("ids", ids),
            ConnStrings.Core,
            ct);

    public Task<KrizaljkaTemplate?> GetByMatrixKeyAsync(string matrixKey, CancellationToken ct) =>
        BaseGetAsync<KrizaljkaTemplate, KrizaljkaTemplateDao>(
            $"select {DaoUtils.GetSelectColumns(typeof(KrizaljkaTemplateDao))} from {Procs.TemplateView} where matrixKey = @key",
            new SqlParams()
                .Add("key", matrixKey),
            ConnStrings.Core,
            ct);

    public Task<PaginatedResult<List<KrizaljkaTemplateListItem>>> GetListAsync(IPaginationCore paginationCore,
        CancellationToken ct) =>
        BaseGetPaginatedListAsync<KrizaljkaTemplateListItem, KrizaljkaTemplateListItemDao>(
            paginationCore,
            Procs.TemplateView,
            KrizaljkaTemplateListItemDao.ToDaoPaginationParameters,
            null,
            ConnStrings.Core,
            ct);

    public Task<string?> UpdateIsActiveAsync(
        long id, 
        bool isActive, 
        string changestamp, 
        CancellationToken ct) =>
        BaseExecuteWithOutAsync<string?>(
            $"call {Procs.TemplateUpdateIsActive}(@id, @isactive, @changestamp,  null);",
            new SqlParams()
                .Add("id", id)
                .Add("isactive", isActive)
                .Add("changestamp", changestamp)
                .AddOutput("newchangestamp", dbType: DbType.String),
            "newchangestamp",
            ConnStrings.Core,
            ct);
}
