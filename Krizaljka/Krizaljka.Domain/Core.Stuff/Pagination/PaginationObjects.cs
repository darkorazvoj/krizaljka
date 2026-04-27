
namespace Krizaljka.Domain.Core.Stuff.Pagination;

public interface IPaginationCore;

public interface IInvalidPagination : IPaginationCore;

public record InvalidPagination : IInvalidPagination;

public interface ISort;
public record Sort(string ColumnName, SortDirection SortDirection) : ISort;
public record SortEmpty : ISort;

public interface ISearchTerm;
public interface ISearchTermNonEmpty: ISearchTerm
{
    string Term { get; }
    SearchType SearchType { get; }
    List<string> SearchColumns { get; }
}
public record SearchTerm(string Term, SearchType SearchType, List<string> SearchColumns) : ISearchTermNonEmpty;
public record SearchTermEmpty : ISearchTerm;

public interface IPaginationOffset : IPaginationCore
{
    int PageSize { get; }
    int Page { get; }
    ISort Sort { get; }
    ISearchTerm SearchTerm { get; }
    bool GetTotalNum { get; }
}

public record PaginationOffset(
    int PageSize,
    int Page,
    ISort Sort,
    ISearchTerm SearchTerm,
    bool GetTotalNum) : IPaginationOffset
{
    public static PaginationOffset Initial() => new(5, 1, new SortEmpty(), new SearchTermEmpty(), false);
}
