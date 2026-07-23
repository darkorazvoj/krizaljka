using Dapper;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.PostgreSql.Postgres.Stuff;
using Krizaljka.PostgreSql.Postgres.Stuff.Extensions;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.PostgreSql.Postgres.Stuff.Utils;
using System.Text.Json;
using Krizaljka.Domain.Terms.LetterNormalizers;

namespace Krizaljka.PostgreSql.Pagination;

internal static class PaginationOffsetUtils
{
    internal static (string whereClause, DynamicParameters dynamicParameters) GetWhereClause(
        List<ISearchTerm> iSearchTerms,
        Dictionary<string, DaoColumn> searchableColumns)
    {
        var dynamicParameters = new DynamicParameters();
        List<string> whereConditions = [];

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

                var searchTermParsedValue = TypesConverter.CastToType(searchTerm.Term.ToLower(), columnType);
                if (searchTermParsedValue is null)
                {
                    continue;
                }

                var columnNameInQuery = originalColumnName;
                if (columnType == typeof(string))
                {
                    columnNameInQuery = columnNameInQuery.SurroundLower();
                }

                var parameterName = $"@{originalColumnName}{counter}search";

                switch (searchTerm.SearchType)
                {
                    case SearchType.Equal:
                        whereConditions.Add($"{columnNameInQuery} = {parameterName}");
                        dynamicParameters.Add(parameterName, searchTermParsedValue);
                        break;
                    case SearchType.NotEqual:
                        whereConditions.Add($"{columnNameInQuery} <> {parameterName}");
                        dynamicParameters.Add(parameterName, searchTermParsedValue);
                        break;
                    case SearchType.StartsWith:
                        whereConditions.Add($"{columnNameInQuery} like {parameterName}");
                        dynamicParameters.Add(parameterName, searchTermParsedValue + "%");
                        break;
                    case SearchType.Contains:
                        whereConditions.Add($"{columnNameInQuery} like {parameterName}");
                        dynamicParameters.Add(parameterName, "%" + searchTermParsedValue + "%");
                        break;
                    case SearchType.Characters:
                        var parsedJsonArray = ParseJsonArrayToSearchValue(searchTermParsedValue);
                        whereConditions.Add($"{columnNameInQuery} like {parameterName}");
                        dynamicParameters.Add(parameterName, parsedJsonArray);
                        break;
                }
                counter++;
            }
        }

        var whereClause = whereConditions.Count > 0
            ? $"WHERE {string.Join(" AND ", whereConditions)}"
            : string.Empty;

        return (whereClause, dynamicParameters);
    }

    private static string ParseJsonArrayToSearchValue(object? searchTermParsedValueObject)
    {
        if (searchTermParsedValueObject is not string str ||
            string.IsNullOrWhiteSpace(str))
        {
            return string.Empty;
        }

        try
        {
            var letters = JsonSerializer.Deserialize<List<string>>(str) ?? [];
            return string.Concat(letters.Select(LettersNormalizer.NormalizeLetter));
        }
        catch
        {
            return string.Empty;
        }
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
        PaginationOffsetParameters paginationParameters,
        string? fixedWhereClause = null) =>
        $"{GetBaseSqlQuery(daoType, viewName)} { GetFinalWhereClause(paginationParameters.WhereClause, fixedWhereClause)} {paginationParameters.OrderByClause} {paginationParameters.PagingClause}";

    private static string GetBaseSqlQuery(Type? daoType, string viewName)
    {
        var selectColumns = daoType is null ? "*" : DaoUtils.GetSelectColumns(daoType);
        return $"select {selectColumns} from {viewName}";
    }

    internal static string GetSqlQueryForTotal(
        string viewName,
        PaginationOffsetParameters paginationParameters,
        string? fixedWhereClause = null) =>
        $"select count(*) as c from {viewName} {GetFinalWhereClause(paginationParameters.WhereClause, fixedWhereClause)}";

    private static string GetFinalWhereClause(string? whereClause, string? fixedWhereClause)
    {
        var finalWhereClause = string.Empty;

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            finalWhereClause = whereClause;

            if (!string.IsNullOrWhiteSpace(fixedWhereClause))
            {
                return $"{finalWhereClause} AND {fixedWhereClause}";
            }
        }

        if (!string.IsNullOrWhiteSpace(fixedWhereClause))
        {
            finalWhereClause = $"WHERE {fixedWhereClause}";
        }

        return finalWhereClause;
    }

}
