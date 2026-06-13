using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Terms;
using Krizaljka.PostgreSql.Postgres.Stuff;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.PostgreSql.Sql;
using System.Data;

namespace Krizaljka.PostgreSql.TermPostgreSql;

internal class TermImportBatchRepo(IDbSession<ConnStrings> dbSession)
    : BaseRepo1<ConnStrings>(dbSession), ITermImportBatchRepo
{
    public Task<long> InsertAsync(
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct) => BaseExecuteWithOutAsync<long>(
        $"call {Procs.TermImportBatchInsert} (@createdOn, @RanById, null);",
        new SqlParams()
            .Add("createdOn", createdOn)
            .Add("ranById", ranById)
            .AddOutput("newId", DbType.Int64),
        "newId",
        ConnStrings.Core,
        ct);
}
