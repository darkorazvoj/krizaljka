
namespace Krizaljka.Domain.Template;

public record KrizaljkaTemplate(
    long Id,
    string? Name,
    int[][] Matrix,
    int RowsCount,
    int ColumnsCount,
    bool IsActive,
    long CreatedById,
    DateTimeOffset? CreatedOn,
    string Changestamp);
