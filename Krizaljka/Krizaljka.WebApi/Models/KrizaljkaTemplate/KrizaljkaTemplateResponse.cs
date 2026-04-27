namespace Krizaljka.WebApi.Models.KrizaljkaTemplate;

public record KrizaljkaTemplateResponse(
    long Id,
    string? Name,
    int[][] Matrix,
    int RowsCount,
    int ColumnsCount,
    bool IsActive,
    long CreatedById,
    DateTimeOffset? CreatedOn,
    string Changestamp);

