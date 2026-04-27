using Krizaljka.Domain.User.Models;
using Krizaljka.Domain.User.Repo;
using Krizaljka.PostgreSql.Postgres.Stuff;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.User;

internal class AppUserRepo(IReadOnlyDictionary<ConnStrings, string> conns) 
    : BaseRepo<ConnStrings>(conns), IAppUserRepo{
    public  Task<AppUserAuth?> GetByLoginEmailAsync(string username, CancellationToken ct) =>
        BaseGetAsync<AppUserAuth, AppUserAuthDao>(
            $"select {DaoUtils.GetSelectColumns(typeof(AppUserAuthDao))} from cr.appUserLoginGet_v1 (@username)",
            new SqlParams()
                .Add("username", username),
            ConnStrings.Core,
            ct);

    public Task IncreaseLoginAttemptAsync(long id, string changestamp, long ranById, CancellationToken ct) =>
        BaseExecuteAsync(
            "call cr.appUserIncreaseLoginAttempt_v1 (@id,@ranById,@changestamp);",
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
            "call cr.appUserIncreaseLoginAttemptAndBlock_v1 (@id,@blockedUntil,@ranById,@changestamp);",
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
            "call cr.appUserUnblock_v1 (@id,@ranById,@changestamp);",
            new SqlParams()
                .Add("id", id)
                .Add("ranById", ranById)
                .Add("changestamp", changestamp),
            ConnStrings.Core,
            ct);

    public Task ResetLoginAttemptsAsync(long id, string changestamp, long ranById, CancellationToken ct) =>
        BaseExecuteAsync(
            "call cr.appUserResetLoginAttempts_v1 (@id,@ranById,@changestamp);",
            new SqlParams()
                .Add("id", id)
                .Add("ranById", ranById)
                .Add("changestamp", changestamp),
            ConnStrings.Core,
            ct);

}
