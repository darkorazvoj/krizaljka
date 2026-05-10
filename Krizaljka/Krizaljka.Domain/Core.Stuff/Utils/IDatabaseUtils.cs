
namespace Krizaljka.Domain.Core.Stuff.Utils;

public interface IDatabaseUtils
{
    public const string InvalidChangestampCode = "CC001";
    public const string ForbiddenCode = "CC005";
    string MapSqlStateToError(string? sqlState);
}
