namespace Krizaljka.WebApi.Models;

public record PaginationOffsetResponse<TList>(TList List, long? Total);
