using Krizaljka.Domain.Idea;
using Krizaljka.PostgreSql.Pagination;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.KrizaljkaIdeaPostgreSql;

internal record KrizaljkaIdeaListItemDao(
    string Id,
    int Status,
    string ThemeName,
    long CreatedById,
    string Changestamp): IDao
{
    private static readonly DaoColumn IdColumn = new("id", typeof(long));

    public static DaoPaginationParameters<KrizaljkaIdeaListItemDao> ToDaoPaginationParameters() =>
        new(
            IdColumn,
            new Dictionary<string, DaoColumn>
            {
                { "id", IdColumn },
                { "status", new DaoColumn("status", typeof(int)) },
                { "themename", new DaoColumn("themename", typeof(string)) },
                { "createdbyid", new DaoColumn("createdbyid", typeof(long)) },
            },
            new Dictionary<string, DaoColumn>
            {
                { "status", new DaoColumn("status", typeof(int)) },
                { "themename", new DaoColumn("themename", typeof(string)) },
                { "createdbyid", new DaoColumn("createdbyid", typeof(long)) },
            });

    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(KrizaljkaIdeaListItem))
        {
            object result = new KrizaljkaIdeaListItem(
                Id,
                (KrizaljkaIdeaStatus)Status,
                ThemeName,
                CreatedById,
                Changestamp);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
