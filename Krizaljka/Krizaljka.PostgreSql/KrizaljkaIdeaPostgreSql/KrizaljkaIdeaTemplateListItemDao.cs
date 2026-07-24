using Krizaljka.Domain.Idea;
using Krizaljka.PostgreSql.Pagination;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.KrizaljkaIdeaPostgreSql;

internal record KrizaljkaIdeaTemplateListItemDao(
    int TemplateType,
    string Id,
    long TemplateArrayId,
    long? TemplateId): IDao
{
    private static readonly DaoColumn IdColumn = new("id", typeof(string));

    public static DaoPaginationParameters<KrizaljkaIdeaTemplateListItemDao> ToDaoPaginationParameters() =>
        new(
            IdColumn,
            new Dictionary<string, DaoColumn>
            {
                { "id", IdColumn },
                { "templatetype", new DaoColumn("templatetype", typeof(TemplateItemType)) },
                { "templatearrayid", new DaoColumn("templatearrayid", typeof(long)) },
                { "templateid", new DaoColumn("templateid", typeof(long)) },
            },
            new Dictionary<string, DaoColumn>
            {
                { "id", IdColumn },
                { "templatetype", new DaoColumn("templatetype", typeof(TemplateItemType)) },
                { "templatearrayid", new DaoColumn("templatearrayid", typeof(long)) },
                { "templateid", new DaoColumn("templateid", typeof(long)) },
            });

    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(KrizaljkaIdeaTemplateListItem))
        {
            object result = new KrizaljkaIdeaTemplateListItem(
                (TemplateItemType)TemplateType,
                Id,
                TemplateArrayId,
                TemplateId);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
