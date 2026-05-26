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
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct);
}
