using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.Domain.Terms;

namespace Krizaljka.PostgreSql.TermPostgreSql;

internal record TermExportDao(
    long Id,
    int LanguageId,
    string Description,
    string RawValue,
    bool IsActive) :IDao
{
    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(TermExport))
        {
            object result = new TermExport(
                Id,
                LanguageId,
                Description,
                RawValue,
                IsActive);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
