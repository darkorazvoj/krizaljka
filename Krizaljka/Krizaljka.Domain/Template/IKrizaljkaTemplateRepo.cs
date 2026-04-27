
namespace Krizaljka.Domain.Template;

public interface IKrizaljkaTemplateRepo
{
    Task<long> InsertAsync(
        int[][] matrix,
        string? name,
        int numOfRows,
        int numOfColumns,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct);

    Task<KrizaljkaTemplate?> GetAsync(long id, CancellationToken ct);
}

