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

        return PaginationOffset.Initial();
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
}


// ?pt=offset