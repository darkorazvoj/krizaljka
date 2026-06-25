using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.Domain.Terms;

namespace Krizaljka.PostgreSql.TermPostgreSql;

internal record TermExportDao(
    long Id,
    string Term,
    string Description
) : IDao
{
    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(TermExportItem))
        {
            object result = new TermExportItem(
                Id,
                Term,
                Description);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
