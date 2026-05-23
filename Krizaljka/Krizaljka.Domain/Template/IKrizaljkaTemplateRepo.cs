
using Krizaljka.Domain.Core.Stuff.Pagination;

namespace Krizaljka.Domain.Template;

public interface IKrizaljkaTemplateRepo
{
    Task<long> InsertAsync(
        int[][] matrix,
        string matrixKey,
        string? name,
        int numOfRows,
        int numOfColumns,
        int numZeroBlocks,
        List<TemplateBlock> zeroBlocks,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct);
    Task<KrizaljkaTemplate?> GetAsync(long id, CancellationToken ct);
    Task<List<KrizaljkaTemplateExport>> GetForExportAsync(List<long> ids, CancellationToken ct);
    Task<KrizaljkaTemplate?> GetByMatrixKeyAsync(string matrixKey, CancellationToken ct);
    Task<PaginatedResult<List<KrizaljkaTemplateListItem>>> GetListAsync(IPaginationCore paginationCore, CancellationToken ct);
    Task<string?> UpdateIsActiveAsync(
        long id, 
        bool isActive, 
        string changestamp,
        CancellationToken ct);
}
