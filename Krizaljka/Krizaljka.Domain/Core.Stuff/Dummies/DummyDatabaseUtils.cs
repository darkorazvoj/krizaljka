using Krizaljka.Domain.Core.Stuff.Utils;

namespace Krizaljka.Domain.Core.Stuff.Dummies;

public class DummyDatabaseUtils : IDatabaseUtils
{
    public string MapSqlStateToError(string? sqlState) => new("InvalidParametersInDatabaseCall");
}
