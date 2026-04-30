
using Krizaljka.Domain.Template;
using Krizaljka.PostgreSql.Pagination;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.KrizaljkaTemplatePostgreSql;

internal record KrizaljkaTemplateListItemDao(
    long Id,
    string? Name,
    int NumRows,
    int NumColumns,
    bool IsActive)
: IDao
{
    private static readonly DaoColumn IdColumn = new("id", typeof(long));

    public static DaoPaginationParameters<KrizaljkaTemplateListItemDao> ToDaoPaginationParameters() =>
        new(
            IdColumn,
            new Dictionary<string, DaoColumn>
            {
                { "id", IdColumn },
                { "name", new DaoColumn("name", typeof(string)) },
                { "numrows", new DaoColumn("numrows", typeof(int)) },
                { "numcolumns", new DaoColumn("numcolumns", typeof(int)) },
                { "isactive", new DaoColumn("isactive", typeof(bool)) }
            },
            new Dictionary<string, DaoColumn>
            {
                { "name", new DaoColumn("name", typeof(string)) },
                { "numrows", new DaoColumn("numrows", typeof(int)) },
                { "numcolumns", new DaoColumn("numcolumns", typeof(int)) },
                { "isactive", new DaoColumn("isactive", typeof(bool)) }
            });

    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(KrizaljkaTemplateListItem))
        {
            object result = new KrizaljkaTemplateListItem(
                Id,
                Name,
                NumRows,
                NumColumns,
                IsActive);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");    }
}
