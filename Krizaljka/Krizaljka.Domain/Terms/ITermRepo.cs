using Krizaljka.Domain.Core.Stuff.Pagination;

namespace Krizaljka.Domain.Terms;

public interface ITermRepo
{
    Task<long> InsertAsync(
        int languageId,
        string description,
        string rawValue,
        string denseValue,
        List<string> letters,
        List<int> spaceIndexes,
        List<int> dashIndexes,
        int length,
        bool isPrivate,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct);

    Task<PaginatedResult<List<TermListItem>>> GetListAsync(IPaginationCore paginationCore, CancellationToken ct);

    Task<Term?> GetAsync(long id, CancellationToken ct);
    Task<string?> UpdateIsActiveAsync(
        long id, 
        bool isActive, 
        string changestamp,
        CancellationToken ct);

    Task<string?> UpdateDescriptionAsync(
        long id,
        string description,
        string changestamp,
        CancellationToken ct);

    Task<string?> UpdateAsync(
        long id,
        int languageId,
        string description,
        string rawValue,
        string denseValue,
        List<string> letters,
        List<int> spaceIndexes,
        List<int> dashIndexes,
        int length,
        string changestamp,
        CancellationToken ct);
}
