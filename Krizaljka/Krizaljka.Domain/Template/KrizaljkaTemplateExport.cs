
namespace Krizaljka.Domain.Template;

public record KrizaljkaTemplateExport(
    long Id,
    string? Name,
    int[][] Rows);
