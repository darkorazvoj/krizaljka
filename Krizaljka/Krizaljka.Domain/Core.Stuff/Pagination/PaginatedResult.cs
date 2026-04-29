namespace Krizaljka.Domain.Core.Stuff.Pagination;

public record PaginatedResult<T>(IPaginationCore Pagination, T Data, long TotalRows, bool HasMorePages);
