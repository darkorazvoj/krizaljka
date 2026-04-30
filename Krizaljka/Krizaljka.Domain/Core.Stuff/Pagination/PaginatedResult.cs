namespace Krizaljka.Domain.Core.Stuff.Pagination;

public record PaginatedResult<T>(T Data, long TotalRows);
