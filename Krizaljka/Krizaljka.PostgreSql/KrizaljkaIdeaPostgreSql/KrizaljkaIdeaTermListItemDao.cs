using Krizaljka.Domain.Idea;
using Krizaljka.Domain.Terms;
using Krizaljka.PostgreSql.Pagination;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.KrizaljkaIdeaPostgreSql;

internal record KrizaljkaIdeaTermListItemDao(
    int TermType,
    string Id,
    long TermId,
    int TermLanguageId,
    int TermLength,
    bool TermIsActive): IDao
{
    private static readonly DaoColumn IdColumn = new("id", typeof(long));

    public static DaoPaginationParameters<KrizaljkaIdeaTermListItemDao> ToDaoPaginationParameters() =>
        new(
            IdColumn,
            new Dictionary<string, DaoColumn>
            {
                { "id", IdColumn },
                { "termtype", new DaoColumn("termtype", typeof(TermItemType)) },
                { "termid", new DaoColumn("termid", typeof(string)) },
                { "termlanguageid", new DaoColumn("termlanguageid", typeof(TermLanguage)) },
                { "termlength", new DaoColumn("termlength", typeof(int)) },
                { "termisactive", new DaoColumn("termisactive", typeof(bool)) },
            },
            new Dictionary<string, DaoColumn>
            {
                { "id", IdColumn },
                { "termtype", new DaoColumn("termtype", typeof(TermItemType)) },
                { "termid", new DaoColumn("termid", typeof(string)) },
                { "termlanguageid", new DaoColumn("termlanguageid", typeof(TermLanguage)) },
                { "termlength", new DaoColumn("termlength", typeof(int)) },
                { "termisactive", new DaoColumn("termisactive", typeof(bool)) },
            });

    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(KrizaljkaIdeaTermListItem))
        {
            object result = new KrizaljkaIdeaTermListItem(
                (TermItemType)TermType,
                Id,
                TermId,
                (TermLanguage)TermLanguageId,
                TermLength,
                TermIsActive);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
