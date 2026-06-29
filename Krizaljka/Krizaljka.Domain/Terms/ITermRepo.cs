using Krizaljka.Domain.Core.Stuff.Pagination;

namespace Krizaljka.Domain.Terms;

public interface ITermRepo
{
    Task<long> InsertAsync(
        int languageId,
        string rawValue,
        string denseValue,
        string searchValue,
        List<string> letters,
        List<int> spaceIndexes,
        List<int> dashIndexes,
        int length,
        bool isPrivate,
        long? batchId,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct);

    Task<PaginatedResult<List<TermListItem>>> GetListAsync(IPaginationCore paginationCore, CancellationToken ct);

    Task<Term?> GetAsync(long id, CancellationToken ct);

    Task<Term?> GetByLanguageAndLettersAsync(
        int languageId,
        List<string> letters,
        CancellationToken ct);

    Task<List<TermExportItem>> GetForExportAsync(int languageId, CancellationToken ct);
    Task<string?> UpdateIsActiveAsync(
        long id, 
        bool isActive, 
        string changestamp,
        CancellationToken ct);

    Task<string?> UpdateTermAsync(
        long id,
        string rawValue,
        string denseValue,
        string searchValue,
        List<string> letters,
        List<int> spaceIndexes,
        List<int> dashIndexes,
        int length,
        string changestamp,
        CancellationToken ct);
}
