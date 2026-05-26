using System.Data;
using Krizaljka.Domain.Terms;
using Krizaljka.PostgreSql.Postgres.Stuff;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.PostgreSql.Sql;

namespace Krizaljka.PostgreSql.TermPostgreSql;

internal class TermRepo(IReadOnlyDictionary<ConnStrings, string> conns)
    : BaseRepo<ConnStrings>(conns), ITermRepo
{
    public Task<long> InsertAsync(
        int languageId,
        string description,
        string rawValue,
        string denseValue,
        List<string> letters,
        List<int> spaceIndexes,
        List<int> dashIndexes,
        int length,
        bool isPrivate,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct) => BaseExecuteWithOutAsync<long>(
        $"call {Procs.TermInsert} (@languageid, @description, @rawValue, @denseValue, @lettersJson, @spaceIndexesJson, @dashIndexesJson, @length, @isActive, @isPrivate, @createdOn, @RanById, null);",
        new SqlParams()
            .Add("languageid", languageId)
            .Add("description", description)
            .Add("rawValue", rawValue)
            .Add("denseValue", denseValue)
            .AddJsonList("lettersJson", letters)
            .AddJsonList("spaceIndexesJson", spaceIndexes)
            .AddJsonList("dashIndexesJson", dashIndexes)
            .Add("length", length)
            .Add("isactive", true)
            .Add("isprivate", isPrivate)
            .Add("createdOn", createdOn)
            .Add("ranById", ranById)
            .AddOutput("newId", DbType.Int64),
        "newId",
        ConnStrings.Core,
        ct);
}
