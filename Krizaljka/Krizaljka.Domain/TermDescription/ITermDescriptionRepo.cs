
using Krizaljka.Domain.Core.Stuff.Pagination;

namespace Krizaljka.Domain.TermDescription;

public interface ITermDescriptionRepo
{
    Task<long> InsertDescriptionAsync(
        long termId,
        string description,
        long? batchId,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct);

    Task<TermDescription?> GetAsync(long id, CancellationToken ct);

    Task<PaginatedResult<List<TermDescriptionListItem>>> GetListAsync(
        IPaginationCore paginationCore,
        CancellationToken ct);

    Task<string?> UpdateAsync(long id, string description, string changestamp, CancellationToken ct);

    Task DeleteAsync(
        long id,
        string changestamp,
        CancellationToken ct);
}
