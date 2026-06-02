namespace Krizaljka.Domain.Terms;

public interface ITermImportBatchRepo
{
    Task<long> InsertAsync(
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct);
}