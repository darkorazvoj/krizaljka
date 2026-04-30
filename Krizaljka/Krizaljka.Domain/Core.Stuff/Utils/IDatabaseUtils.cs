
namespace Krizaljka.Domain.Core.Stuff.Utils;

public interface IDatabaseUtils
{
    public const string InvalidChangestampCode = "CC001";
    string MapSqlStateToError(string? sqlState);
}
