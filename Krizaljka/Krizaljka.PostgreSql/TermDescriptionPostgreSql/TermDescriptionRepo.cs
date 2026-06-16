using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.PostgreSql.Postgres.Stuff;
using System.Data;
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
}
