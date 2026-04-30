using Dapper;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.PostgreSql.Postgres.Stuff;
using Krizaljka.PostgreSql.Postgres.Stuff.Extensions;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.Pagination;

internal static class PaginationOffsetUtils
{
    internal static (string whereClause, DynamicParameters dynamicParameters) GetWhereClause(
        List<ISearchTerm> iSearchTerms,
        Dictionary<string, DaoColumn> searchableColumns)
    {
        var dynamicParameters = new DynamicParameters();
        List<string> searchConditions = [];

        var counter = 1;
        foreach (var iSearchTerm in iSearchTerms)
        {
            if (iSearchTerm is not SearchTerm searchTerm)
            {
                continue;
            }

            var columns = searchTerm.SearchColumns.Count != 0
                ? searchTerm.SearchColumns
                : searchableColumns.Keys.ToList();

            foreach (var columnName in columns)
            {
                if (!searchableColumns.TryGetValue(columnName, out var column))
                {
                    continue;
                }

                var (originalColumnName, columnType) = column;

                var columnNameInQuery = originalColumnName;
                if (columnType == typeof(string))
                {
                    columnNameInQuery = columnNameInQuery.SurroundLower();
                }

                var parameterName = $"@{originalColumnName}{counter}search";
                switch (searchTerm.SearchType)
                {
                    case SearchType.Equal:
                        searchConditions.Add($"{columnNameInQuery} = {parameterName}");
                        dynamicParameters.Add(parameterName, searchTerm.Term.ToLower());
                        break;
                    case SearchType.NotEqual:
                        searchConditions.Add($"{columnNameInQuery} <> {parameterName}");
                        dynamicParameters.Add(parameterName, searchTerm.Term.ToLower());
                        break;
                    case SearchType.StartsWith:
                        searchConditions.Add($"{columnNameInQuery} like {parameterName}");
                        dynamicParameters.Add(parameterName, searchTerm.Term.ToLower() + "%");
                        break;
                    case SearchType.Contains:
                        searchConditions.Add($"{columnNameInQuery} like {parameterName}");
                        dynamicParameters.Add(parameterName, "%" + searchTerm.Term.ToLower() + "%");
                        break;
                }

                counter++;
            }
        }

        var whereClause = searchConditions.Count > 0
            ? $"WHERE {string.Join(" AND ", searchConditions)}"
            : string.Empty;

        return (whereClause, dynamicParameters);
    }

    internal static string GetOrderByClause(
        IPaginationCore paginationCore,
        DaoColumn idColumn,
        Dictionary<string, DaoColumn> columnNicknameDaoColumn)
    {
        switch (paginationCore)
        {

            case PaginationOffset paginationOffset:
                if(paginationOffset.Sort is Sort sort)
                {
                    if(columnNicknameDaoColumn.TryGetValue(sort.ColumnName.ToLower(), out var daoColumn))
                    {
                        return $"ORDER BY {daoColumn.ColumnName} {sort.SortDirection.ToString().ToUpper()}";
                    }
                }

                return  $"ORDER BY {idColumn.ColumnName} ASC";
            default:
                return string.Empty;
        }
    }

    internal static (string pagingClause, DynamicParameters dynamicParameters) GetPagingClause(int page, int pageSize)
    {
        var dynamicParameters = new DynamicParameters();
        const string pagingClause = " LIMIT @pageSize OFFSET @offset";

        dynamicParameters.Add("pageSize", pageSize);
        dynamicParameters.Add("offset", (page - 1) * pageSize);

        return (pagingClause, dynamicParameters);
    }

    internal static string GetSqlQuery(
        Type daoType,
        string viewName, 
        PaginationOffsetParameters paginationParameters) =>
        $"{GetBaseSqlQuery(daoType, viewName)} {paginationParameters.WhereClause} {paginationParameters.OrderByClause} {paginationParameters.PagingClause}";

    private static string GetBaseSqlQuery(Type? daoType, string viewName)
    {
        var selectColumns = daoType is null ? "*" : DaoUtils.GetSelectColumns(daoType);
        return $"select {selectColumns} from {viewName}";
    }

    internal static string GetSqlQueryForTotal(
        string viewName,
        PaginationOffsetParameters paginationParameters) =>
        $"select count(*) as c from {viewName} {paginationParameters.WhereClause}";

}
