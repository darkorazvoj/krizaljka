
namespace Krizaljka.Domain.TemplateAnalysis;

public record KrizaljkaSlot(
    int Id,
    KrizaljkaDirection Direction,
    int Row,
    int Col,
    int Length,
    IReadOnlyList<KrizaljkaCell> Cells,
    IReadOnlyList<(int Row, int Col)> CellKeys);
