using Krizaljka.Domain.TermDescription;
using Krizaljka.PostgreSql.Pagination;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.TermDescriptionPostgreSql;

internal record TermDescriptionListItemDao(
    long Id,
    long TermId,
    string Description,
    long BatchId,
    long CreatedById,
    string Changestamp) : IDao
{
    private static readonly DaoColumn IdColumn = new("id", typeof(long));

    public static DaoPaginationParameters<TermDescriptionListItemDao> ToDaoPaginationParameters() =>
        new(
            IdColumn,
            new Dictionary<string, DaoColumn>
            {
                { "id", IdColumn },
                { "termid", new DaoColumn("termid", typeof(long)) },
                { "description", new DaoColumn("description", typeof(string)) },
                { "batchid", new DaoColumn("batchid", typeof(long)) },
                { "createdbyid", new DaoColumn("createdbyid", typeof(long)) },
            },
            new Dictionary<string, DaoColumn>
            {
                { "termid", new DaoColumn("termid", typeof(long)) },
                { "description", new DaoColumn("description", typeof(string)) },
                { "batchid", new DaoColumn("batchid", typeof(long)) },
                { "createdbyid", new DaoColumn("createdbyid", typeof(long)) },
            });

    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(TermDescriptionListItem))
        {
            object result = new TermDescriptionListItem(Id, TermId, Description, BatchId, CreatedById, Changestamp);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}

