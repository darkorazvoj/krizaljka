
namespace Krizaljka.Domain.Template;

public interface IKrizaljkaTemplateRepo
{
    Task<long> InsertAsync(
        int[][] matrix,
        string? name,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct);
}
