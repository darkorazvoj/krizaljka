
using Krizaljka.Domain.Template;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.KrizaljkaTemplatePostgreSql;

internal record KrizaljkaTemplateExportDao(
    long Id,
    string? Name,
    int[][] Matrix) : IDao
{ 
    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(KrizaljkaTemplateExport))
        {
            object result = new KrizaljkaTemplateExport(Id, Name, Matrix);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
