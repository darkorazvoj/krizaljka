using Dapper;
using Krizaljka.Domain.Core.Stuff.Pagination;

namespace Krizaljka.PostgreSql.Pagination;

internal static class PaginationUtils
{
    internal static PaginationOffsetParameters GetPaginationParameters<T>(
        IPaginationCore paginationCore,
        DaoPaginationParameters<T> daoPaginationParameters)
    {
        var whereClause = string.Empty;
        var orderByClause = string.Empty;
        var pagingClause = string.Empty;
        var getTotal = false;

        var dynamicParameters = new DynamicParameters();

        switch (paginationCore)
        {
            case PaginationOffset paginationOffset:
                (whereClause, var searchTermsParameters) = PaginationOffsetUtils.GetWhereClause(paginationOffset.SearchTerms, daoPaginationParameters.SearchableColumns);
                dynamicParameters.AddDynamicParams(searchTermsParameters);

                orderByClause = PaginationOffsetUtils.GetOrderByClause(
                    paginationOffset,
                    daoPaginationParameters.IdColumn,
                    daoPaginationParameters.Mappings);

                (pagingClause, var pagingParameters) =
                    PaginationOffsetUtils.GetPagingClause(paginationOffset.Page, paginationOffset.PageSize);
                dynamicParameters.AddDynamicParams(pagingParameters);

                getTotal = paginationOffset.GetTotalNum;

                break;
        }

        return new PaginationOffsetParameters(
            whereClause,
            orderByClause,
            pagingClause,
            getTotal,
            dynamicParameters);
    }
}
