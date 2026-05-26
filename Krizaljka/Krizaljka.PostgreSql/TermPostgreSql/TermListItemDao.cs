using Krizaljka.PostgreSql.Pagination;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.Domain.Terms;

namespace Krizaljka.PostgreSql.TermPostgreSql;

internal record TermListItemDao(
    long Id,
    int LanguageId,
    string RawValue,
    int TermLength,
    bool IsActive,
    long CreatedById) : IDao
{
    private static readonly DaoColumn IdColumn = new("id", typeof(long));

    public static DaoPaginationParameters<TermListItemDao> ToDaoPaginationParameters() =>
        new(
            IdColumn,
            new Dictionary<string, DaoColumn>
            {
                { "id", IdColumn },
                { "languageid", new DaoColumn("languageid", typeof(int)) },
                { "description", new DaoColumn("description", typeof(string)) },
                { "rawvalue", new DaoColumn("rawvalue", typeof(string)) },
                { "termlength", new DaoColumn("termlength", typeof(int)) },
                { "isactive", new DaoColumn("isactive", typeof(bool)) },
                { "createdbyid", new DaoColumn("createdbyid", typeof(long)) },
            },
            new Dictionary<string, DaoColumn>
            {
                { "languageid", new DaoColumn("languageid", typeof(int)) },
                { "description", new DaoColumn("description", typeof(string)) },
                { "rawvalue", new DaoColumn("rawvalue", typeof(string)) },
                { "termlength", new DaoColumn("termlength", typeof(int)) },
                { "isactive", new DaoColumn("isactive", typeof(bool)) },
                { "createdbyid", new DaoColumn("createdbyid", typeof(long)) },
            });

    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(TermListItem))
        {
            object result = new TermListItem(Id, (TermLanguage)LanguageId, RawValue, TermLength, IsActive, CreatedById);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}

