using Dapper;
using Krizaljka.Domain.Core.Stuff.Pagination;

namespace Krizaljka.PostgreSql.Pagination;

internal static class PaginationUtils
{
    internal static PaginationParameters GetPaginationParameters<T>(
        IPaginationCore paginationCore,
        DaoPaginationParameters<T> daoPaginationParameters)
    {
        var whereClause = string.Empty;
        var orderByClause = string.Empty;
        var limitClause = string.Empty;
        List<string> searchTerms = [];
        var getTotal = false;

        var dynamicParameters = new DynamicParameters();

        switch (paginationCore)
        {
            case PaginationOffset paginationOffset:
                var (searchTermsParsed, searchTermsParameters) = PaginationOffsetUtils.GetWhereClause(paginationOffset.SearchTerm, daoPaginationParameters.SearchableColumns);
                searchTerms.AddRange(searchTermsParsed);
                dynamicParameters.AddDynamicParams(searchTermsParameters);

                break;
        }

        return new PaginationParameters(
            whereClause,
            orderByClause,
            limitClause,
            searchTerms ?? [],
            getTotal,
            dynamicParameters);
    }
}
