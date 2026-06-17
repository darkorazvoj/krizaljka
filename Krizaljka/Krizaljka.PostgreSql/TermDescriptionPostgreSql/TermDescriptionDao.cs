using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.Domain.TermDescription;

namespace Krizaljka.PostgreSql.TermDescriptionPostgreSql;

internal record TermDescriptionDao(
    long Id,
    long TermId,
    string Description,
    long BatchId,
    long CreatedById,
    DateTimeOffset? CreatedOn,
    string Changestamp) : IDao
{
    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(TermDescription))
        {
            object result = new TermDescription(
                Id,
                TermId,
                Description,
                BatchId,
                CreatedById,
                CreatedOn,
                Changestamp);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}

