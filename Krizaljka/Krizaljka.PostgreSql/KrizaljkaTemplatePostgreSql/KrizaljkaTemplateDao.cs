
using Krizaljka.Domain.Template;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.KrizaljkaTemplatePostgreSql;

internal record KrizaljkaTemplateDao(
    long Id,
    string? Name,
    int[][] Matrix,
    int NumRows,
    int NumColumns,
    bool IsActive,
    long CreatedById,
    DateTimeOffset? CreatedOn,
    string Changestamp) : IDao
{ 
    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(KrizaljkaTemplate))
        {
            object result = new KrizaljkaTemplate(
                Id,
                Name,
                Matrix,
                NumRows,
                NumColumns,
                IsActive,
                CreatedById,
                CreatedOn,
                Changestamp);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
