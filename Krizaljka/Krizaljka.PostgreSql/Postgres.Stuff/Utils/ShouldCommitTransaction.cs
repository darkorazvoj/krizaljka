using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.PostgreSql.Postgres.Stuff.Utils;

internal static class ShouldCommitTransaction
{
    public static bool ForResult(IServiceResult result) => result is ICommittableResult;
}
