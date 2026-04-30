namespace Krizaljka.WebApi.Models.KrizaljkaTemplate;

public record KrizaljkaTemplateListItemResponse(
    long Id,
    string? Name,
    int RowsCount,
    int ColumnsCount,
    bool IsActive);
