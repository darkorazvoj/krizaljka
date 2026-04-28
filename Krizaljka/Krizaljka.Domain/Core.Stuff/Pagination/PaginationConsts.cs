namespace Krizaljka.Domain.Core.Stuff.Pagination;

public class PaginationConsts
{
    public const int PaginationLimitDefault = 5;

    public const string SortDirectionAsc = "asc";
    public const string SortDirectionDesc = "desc";

    public const string PaginationTypeQueryStringKey = "pt";
    public const string PaginationTypeOffsetQueryStringValue = "offset";

    public const string PageSizeQueryStringKey = "pageSize";
    public const string PageQueryStringKey = "page";
    public const string GetTotalNumQueryStringKey = "gtn";
    public const string SortQueryStringKey = "sort";
    public const string SearchTermQueryStringKey = "search";

}
