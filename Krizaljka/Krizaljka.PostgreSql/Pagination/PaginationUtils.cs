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
        var limitClause = string.Empty;
        //List<string> searchTerms = [];
        var getTotal = false;

        var dynamicParameters = new DynamicParameters();

        switch (paginationCore)
        {
            case PaginationOffset paginationOffset:
                (whereClause, var searchTermsParameters) = PaginationOffsetUtils.GetWhereClause(paginationOffset.SearchTerm, daoPaginationParameters.SearchableColumns);
                //searchTerms.AddRange(searchTermsParsed);
                dynamicParameters.AddDynamicParams(searchTermsParameters);

                orderByClause = PaginationOffsetUtils.GetOrderByClause(
                    paginationOffset,
                    daoPaginationParameters.IdColumn,
                    daoPaginationParameters.Mappings);

                limitClause = PaginationOffsetUtils.GetLimitClause(paginationOffset.PageSize);
                getTotal = paginationOffset.GetTotalNum;

                break;
        }

        return new PaginationOffsetParameters(
            whereClause,
            orderByClause,
            limitClause,
           // searchTerms,
            getTotal,
            dynamicParameters);
    }
}
