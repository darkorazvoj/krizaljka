
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.Pagination;

internal record DaoPaginationParameters<T>(
    DaoColumn IdColumn,
    Dictionary<string, DaoColumn> Mappings,
    Dictionary<string, DaoColumn> SearchableColumns);
