
using Krizaljka.Domain.Template;
using Krizaljka.PostgreSql.Postgres.Stuff;

namespace Krizaljka.PostgreSql.KrizaljkaTemplatePostgreSql;

internal class KrizaljkaTemplateRepo(IReadOnlyDictionary<ConnStrings, string> conns): BaseRepo<ConnStrings>(conns), IKrizaljkaTemplateRepo
{
    public async Task<long> InsertAsync(
        int[][] matrix,
        string? name,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct) =>
    await BaseExecuteWithOutAsync<long>(
    $"call {Procs.FlAppUserInsert}(@canlogin, @loginemail, @passwordhash, @isblocked, @blockeduntil, @firstname, @lastname, @isactive, @canCreateTenant, @joinedOn, @ranById, null);",
    new SqlParams()
        .Add("canlogin", canLogin)
    .Add("loginemail", loginEmail)
    .Add("passwordhash", passwordHash)
    .Add("isblocked", false)
        .Add("blockeduntil", null, DbType.DateTimeOffset)
    .Add("firstname", firstName)
    .Add("lastname", lastName)
    .Add("isactive", true)
        .Add("canCreateTenant", canCreateTenant)
    .Add("joinedon", joinedOn)
    .Add("ranById", (int)ranById)
    .AddOutput("newId", DbType.Int64),
    "newId",
    ConnStrings.Core,
    ct);
}
