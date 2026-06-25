using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Terms;
using Krizaljka.PostgreSql.Postgres.Stuff;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.PostgreSql.Sql;
using System.Data;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;

namespace Krizaljka.PostgreSql.TermPostgreSql;

internal class TermRepo(IDbSession<ConnStrings> dbSession)
    : BaseRepo<ConnStrings>(dbSession), ITermRepo
{
    public Task<long> InsertAsync(
        int languageId,
        string rawValue,
        string denseValue,
        List<string> letters,
        List<int> spaceIndexes,
        List<int> dashIndexes,
        int length,
        bool isPrivate,
        long? batchId,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct) => BaseExecuteWithOutAsync<long>(
        $"call {Procs.TermInsert} (@languageid, @rawValue, @denseValue, @lettersJson, @spaceIndexesJson, @dashIndexesJson, @length, @isActive, @isPrivate, @batchId, @createdOn, @RanById, null);",
        new SqlParams()
            .Add("languageid", languageId)
            .Add("rawValue", rawValue)
            .Add("denseValue", denseValue)
            .AddJsonList("lettersJson", letters)
            .AddJsonList("spaceIndexesJson", spaceIndexes)
            .AddJsonList("dashIndexesJson", dashIndexes)
            .Add("length", length)
            .Add("isactive", true)
            .Add("isprivate", isPrivate)
            .Add("batchId", batchId)
            .Add("createdOn", createdOn)
            .Add("ranById", ranById)
            .AddOutput("newId", DbType.Int64),
        "newId",
        ConnStrings.Core,
        ct);

    public Task<PaginatedResult<List<TermListItem>>> GetListAsync(IPaginationCore paginationCore,
        CancellationToken ct) =>
        BaseGetPaginatedListAsync<TermListItem, TermListItemDao>(
            paginationCore,
            Procs.TermView,
            TermListItemDao.ToDaoPaginationParameters,
            "isPrivate = false",
            ConnStrings.Core,
            ct);

    public Task<Term?> GetByLanguageAndLettersAsync(
        int languageId,
        List<string> letters,
        CancellationToken ct) =>
        BaseGetAsync<Term, TermDao>(
            $"select {DaoUtils.GetSelectColumns(typeof(TermDao))} from {Procs.TermView} where languageid = @languageId AND letters = @lettersJson",
            new SqlParams()
                .Add("languageId", languageId)
                .AddJsonList("lettersJson", letters),
            ConnStrings.Core,
            ct);

    public Task<Term?> GetAsync(long id, CancellationToken ct) =>
        BaseGetAsync<Term, TermDao>(
            $"select {DaoUtils.GetSelectColumns(typeof(TermDao))} from {Procs.TermView} where id = @id",
            new SqlParams()
                .Add("id", id),
            ConnStrings.Core,
            ct);

    public Task<List<TermExportItem>> GetForExportAsync(int languageId, CancellationToken ct) =>
        BaseGetListAsync<TermExportItem, TermExportDao>(
            $"select {DaoUtils.GetSelectColumns(typeof(TermExportDao))} from {Procs.TermExportView} where languageId = @languageId;",
            new SqlParams()
                .Add("languageId", languageId),
            ConnStrings.Core,
            ct);

    public Task<string?> UpdateIsActiveAsync(
        long id, 
        bool isActive, 
        string changestamp, 
        CancellationToken ct) =>
        BaseExecuteWithOutAsync<string?>(
            $"call {Procs.TermUpdateIsActive}(@id, @isactive, @changestamp,  null);",
            new SqlParams()
                .Add("id", id)
                .Add("isactive", isActive)
                .Add("changestamp", changestamp)
                .AddOutput("newchangestamp", dbType: DbType.String),
            "newchangestamp",
            ConnStrings.Core,
            ct);

   

    public Task<string?> UpdateTermAsync(
        long id,
        string rawValue,
        string denseValue,
        List<string> letters,
        List<int> spaceIndexes,
        List<int> dashIndexes,
        int length,
        string changestamp,
        CancellationToken ct) =>
        BaseExecuteWithOutAsync<string?>(
            $"call {Procs.TermUpdateTerm}(@id, @rawValue, @denseValue, @lettersJson, @spaceIndexesJson, @dashIndexesJson, @length, @changestamp,  null);",
            new SqlParams()
                .Add("id", id)
                .Add("rawValue", rawValue)
                .Add("denseValue", denseValue)
                .AddJsonList("lettersJson", letters)
                .AddJsonList("spaceIndexesJson", spaceIndexes)
                .AddJsonList("dashIndexesJson", dashIndexes)
                .Add("length", length)
                .Add("changestamp", changestamp)
                .AddOutput("newchangestamp", dbType: DbType.String),
            "newchangestamp",
            ConnStrings.Core,
            ct);
}
