using Krizaljka.Domain.Idea;
using Krizaljka.PostgreSql.Pagination;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.KrizaljkaIdeaPostgreSql;

internal record KrizaljkaIdeaAllTemplatesListItemDao(
    string Id,
    long? TemplateId,
    string? Name,
    bool? IsActive): IDao
{
    private static readonly DaoColumn IdColumn = new("id", typeof(string));

    public static DaoPaginationParameters<KrizaljkaIdeaAllTemplatesListItemDao> ToDaoPaginationParameters() =>
        new(
            IdColumn,
            new Dictionary<string, DaoColumn>
            {
                { "id", IdColumn },
                { "templateid", new DaoColumn("templateid", typeof(long)) },
                { "name", new DaoColumn("name", typeof(string)) },
                { "isactive", new DaoColumn("name", typeof(bool)) },
            },
            new Dictionary<string, DaoColumn>
            {
                { "id", IdColumn },
                { "templateid", new DaoColumn("templateid", typeof(long)) },
                { "isactive", new DaoColumn("name", typeof(bool)) },
            });

    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(KrizaljkaIdeaAllTemplatesListItem))
        {
            object result = new KrizaljkaIdeaAllTemplatesListItem(
                Id,
                TemplateId,
                Name,
                IsActive);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
