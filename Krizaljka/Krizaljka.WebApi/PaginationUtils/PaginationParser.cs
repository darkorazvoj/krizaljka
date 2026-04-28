using Krizaljka.Domain.Core.Stuff.Extensions;
using Krizaljka.Domain.Core.Stuff.Pagination;

namespace Krizaljka.WebApi.PaginationUtils;

internal static class PaginationParser
{
    public static IPaginationCore Parse(string? paginationQueryStringBase64)
    {
        if (string.IsNullOrWhiteSpace(paginationQueryStringBase64))
        {
            return PaginationOffset.Initial();
        }

        try
        {
            var queryStringValues = ParseQueryString(paginationQueryStringBase64);

            if (!queryStringValues.TryGetValue(PaginationConsts.PaginationTypeQueryStringKey, out var paginationType))
            {
                return PaginationOffset.Initial();
            }

            return paginationType.Trim().ToLower() switch
            {
                PaginationConsts.PaginationTypeOffsetQueryStringValue => GetOffsetPagination(queryStringValues),
                _ => PaginationOffset.Initial()
            };

        }
        catch
        {
            return PaginationOffset.Initial();
        }
    }

    private static Dictionary<string, string> ParseQueryString(string paginationQueryStringBase64 )
    {
        Dictionary<string, string> queryStringValues = [];

        var paginationQueryString = paginationQueryStringBase64.ConvertFromBase64StringSafe();
        var paginationQueryStringArray = paginationQueryString
            .Split('&')
            .ToList();

        
        foreach (var keyValue in paginationQueryStringArray)
        {
            var keyValueArray = keyValue.Split('=');

            if (keyValueArray.Length == 2)
            {
                if (!queryStringValues.ContainsKey(keyValueArray[0]))
                {
                    queryStringValues.Add(keyValueArray[0], keyValueArray[1]);
                }
            }
        }

        return queryStringValues;
    }

    private static PaginationOffset GetOffsetPagination(IReadOnlyDictionary<string, string> queryStringValues)
    {
        var pageSize = ParseIntValueValue(PaginationConsts.PageSizeQueryStringKey, queryStringValues) ??
                       PaginationConsts.PaginationLimitDefault;
        var page = ParseIntValueValue(PaginationConsts.PageQueryStringKey, queryStringValues) ?? 1;
        var getTotalNum = ParseBoolValueValue(PaginationConsts.GetTotalNumQueryStringKey, queryStringValues) ?? false;
        var sort = ParseSort(queryStringValues);
        var searchTerm = ParseSearchTerm(queryStringValues);

        return new PaginationOffset(pageSize, page, sort, searchTerm, getTotalNum);
    }

    private static int? ParseIntValueValue(string key, IReadOnlyDictionary<string, string> queryStringValues)
    {
        if (!queryStringValues.TryGetValue(key, out var valueString))
        {
            return null;
        }

        if (int.TryParse(valueString, out var value))
        {
            return value;
        }

        return null;
    }

    private static bool? ParseBoolValueValue(string key, IReadOnlyDictionary<string, string> queryStringValues)
    {
        if (!queryStringValues.TryGetValue(key, out var valueString))
        {
            return null;
        }

        if (bool.TryParse(valueString, out var value))
        {
            return value;
        }

        return null;
    }

    private static ISort ParseSort(IReadOnlyDictionary<string, string> queryStringValues)
    {
        queryStringValues.TryGetValue(PaginationConsts.SortQueryStringKey, out var sortString);

        if (string.IsNullOrWhiteSpace(sortString))
        {
            return new SortEmpty();
        }

        var elements = sortString.Split(':');
        if (elements.Length != 2)
        {
            return new SortEmpty();
        }

        var sortDirectionValue = elements[1].Trim().ToLower();
        return sortDirectionValue switch
        {
            PaginationConsts.SortDirectionAsc => new Sort(elements[0], SortDirection.Asc),
            PaginationConsts.SortDirectionDesc => new Sort(elements[0], SortDirection.Desc),
            _ => new SortEmpty()
        };
    }

    private static ISearchTerm ParseSearchTerm(
        IReadOnlyDictionary<string, string> queryStringValues)
    {
        queryStringValues.TryGetValue(PaginationConsts.SearchTermQueryStringKey, out var searchTermString);

        if (string.IsNullOrWhiteSpace(searchTermString))
        {
            return new SearchTermEmpty();
        }

        var elements = searchTermString.Split(':');
        switch (elements.Length)
        {
            case 1:
                return new SearchTerm(elements[0], SearchType.StartsWith, []);
            case 2:
                var searchTypeParsed2 = ParseSearchTypeValue(elements[1]);
                return searchTypeParsed2 is null
                    ? new SearchTermEmpty()
                    : new SearchTerm(elements[0], searchTypeParsed2.Value, []);
            case 3:
            {
                var searchTypeParsed3 = ParseSearchTypeValue(elements[1]);
                if (searchTypeParsed3 is null)
                {
                    return new SearchTermEmpty();
                }

                List<string> searchColumns = [];

                if (!string.IsNullOrWhiteSpace(elements[2]))
                {
                    searchColumns.AddRange(elements[2].Split(',').ToList());
                }

                return new SearchTerm(elements[0], searchTypeParsed3.Value, searchColumns);
            }
            default:
                return new SearchTermEmpty();
        }
    }

    private static SearchType? ParseSearchTypeValue(string searchTypeStringValue) =>
        searchTypeStringValue switch
        {
            "e" => SearchType.Equal,
            "ne" => SearchType.NotEqual,
            "sw" => SearchType.StartsWith,
            "c" => SearchType.Contains,
            _ => null
        };
}


// ?pt=offset