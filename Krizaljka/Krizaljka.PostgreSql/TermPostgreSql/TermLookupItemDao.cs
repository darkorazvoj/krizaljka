using Krizaljka.PostgreSql.Pagination;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.Domain.Terms;

namespace Krizaljka.PostgreSql.TermPostgreSql;

internal record TermLookupItemDao(
    long Id,
    string RawValue,
    int TermLength) : IDao
{
    private static readonly DaoColumn IdColumn = new("id", typeof(long));

    public static DaoPaginationParameters<TermLookupItemDao> ToDaoPaginationParameters() =>
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
                { "letters", new DaoColumn("searchvalue", typeof(string)) },
            });

    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(TermLookupItem))
        {
            object result = new TermLookupItem(Id, RawValue, TermLength);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}

