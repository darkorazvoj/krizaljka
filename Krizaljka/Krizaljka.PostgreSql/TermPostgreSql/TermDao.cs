using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.Domain.Terms;

namespace Krizaljka.PostgreSql.TermPostgreSql;

internal record TermDao(
    long Id,
    int LanguageId,
    long NumOfDesc,
    string RawValue,
    string DenseValue,
    string SearchValue,
    List<string> Letters,
    List<int> SpaceIndexes,
    List<int> DashIndexes,
    int TermLength,
    bool IsActive,
    bool IsPrivate,
    long? BatchId,
    long CreatedById,
    DateTimeOffset CreatedOn,
    string Changestamp) :IDao
{
    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(Term))
        {
            object result = new Term(
                Id,
                (TermLanguage)LanguageId,
                NumOfDesc,
                RawValue,
                DenseValue,
                SearchValue,
                Letters,
                SpaceIndexes,
                DashIndexes,
                TermLength,
                IsActive,
                IsPrivate,
                BatchId,
                CreatedById,
                CreatedOn,
                Changestamp);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
