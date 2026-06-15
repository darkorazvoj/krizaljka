using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.Domain.Core.Stuff.DatabaseStuff;

public interface IDatabaseUtils
{
    public const string InvalidChangestampCode = "CC001";
    public const string ForbiddenCode = "CC005";
    string MapSqlStateToError(string? sqlState);

    IServiceResult GetSqlErrorResult(string? sqlState)
    {
        return sqlState == InvalidChangestampCode
            ? new InvalidChangestamp()
            : new InvalidRequestWithReason(MapSqlStateToError(sqlState));
    }
}
