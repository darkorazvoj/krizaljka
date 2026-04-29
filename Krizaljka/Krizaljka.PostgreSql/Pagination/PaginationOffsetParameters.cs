using Dapper;

namespace Krizaljka.PostgreSql.Pagination;

public record PaginationOffsetParameters(
    string WhereClause,
    string OrderByClause,
    string PagingClause,
    // List<string> SearchTerms,
    bool GetTotal,
    DynamicParameters DynamicParameters);
