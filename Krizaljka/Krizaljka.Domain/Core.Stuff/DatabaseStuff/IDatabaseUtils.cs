namespace Krizaljka.Domain.Core.Stuff.DatabaseStuff;

public interface IDatabaseUtils
{
    public const string InvalidChangestampCode = "CC001";
    public const string ForbiddenCode = "CC005";
    string MapSqlStateToError(string? sqlState);
}
