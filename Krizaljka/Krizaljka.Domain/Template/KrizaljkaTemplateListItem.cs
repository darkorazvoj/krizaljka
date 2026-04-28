namespace Krizaljka.Domain.Template;

public record KrizaljkaTemplateListItem(
    long Id,
    string? Name,
    int RowsCount,
    int ColumnsCount,
    bool IsActive);