using Krizaljka.Domain.User.Models;
using Krizaljka.Domain.User.Repo;
using Krizaljka.PostgreSql.Postgres.Stuff;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.PostgreSql.Sql;

namespace Krizaljka.PostgreSql.User;

internal class AppUserRepo(IReadOnlyDictionary<ConnStrings, string> conns) 
    : BaseRepo<ConnStrings>(conns), IAppUserRepo{
    public  Task<AppUserAuth?> GetByLoginEmailAsync(string username, CancellationToken ct) =>
        BaseGetAsync<AppUserAuth, AppUserAuthDao>(
            $"select {DaoUtils.GetSelectColumns(typeof(AppUserAuthDao))} from {Procs.AppUserLoginGet} (@username)",
            new SqlParams()
                .Add("username", username),
            ConnStrings.Core,
            ct);

    public Task IncreaseLoginAttemptAsync(long id, string changestamp, long ranById, CancellationToken ct) =>
        BaseExecuteAsync(
            $"call {Procs.AppUserIncreaseLoginAttempt} (@id,@ranById,@changestamp);",
            new SqlParams()
                .Add("id", id)
                .Add("ranById", ranById)
                .Add("changestamp", changestamp),
            ConnStrings.Core,
            ct);

    public Task IncreaseLoginAttemptAndBlockAsync(
        long id, 
        DateTimeOffset blockedUntil, 
        string changestamp,
        long ranById, 
        CancellationToken ct) =>
        BaseExecuteAsync(
            $"call {Procs.AppUserIncreaseLoginAttemptAndBlock} (@id,@blockedUntil,@ranById,@changestamp);",
            new SqlParams()
                .Add("id", id)
                .Add("blockedUntil", blockedUntil)
                .Add("ranById", ranById)
                .Add("changestamp", changestamp),
            ConnStrings.Core,
            ct);

    public Task UnblockAsync(long id,
        string changestamp,
        long ranById,
        CancellationToken ct) =>
        BaseExecuteAsync(
            $"call {Procs.AppUserUnblock} (@id,@ranById,@changestamp);",
            new SqlParams()
                .Add("id", id)
                .Add("ranById", ranById)
                .Add("changestamp", changestamp),
            ConnStrings.Core,
            ct);

    public Task ResetLoginAttemptsAsync(long id, string changestamp, long ranById, CancellationToken ct) =>
        BaseExecuteAsync(
            $"call {Procs.AppUserResetLoginAttempts} (@id,@ranById,@changestamp);",
            new SqlParams()
                .Add("id", id)
                .Add("ranById", ranById)
                .Add("changestamp", changestamp),
            ConnStrings.Core,
            ct);

}
