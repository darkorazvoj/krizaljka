
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
}
