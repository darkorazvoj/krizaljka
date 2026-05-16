
namespace Krizaljka.Domain.Template;

public record KrizaljkaTemplate(
    long Id,
    string? Name,
    int[][] Matrix,
    string MatrixKey,
    int RowsCount,
    int ColumnsCount,
    bool IsActive,
    long CreatedById,
    DateTimeOffset? CreatedOn,
    string Changestamp);
