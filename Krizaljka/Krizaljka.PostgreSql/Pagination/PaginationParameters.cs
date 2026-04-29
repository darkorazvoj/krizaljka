using Dapper;

namespace Krizaljka.PostgreSql.Pagination;

public record PaginationParameters(
    string WhereClause,
    string OrderByClause,
    string LimitClause,
    List<string> SearchTerms,
    bool GetTotal,
    DynamicParameters DynamicParameters);
