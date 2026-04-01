
namespace Krizaljka.Domain.TemplateAnalysis;

public record KrizaljkaSlot(
    long Id,
    KrizaljkaDirection Direction,
    int Row,
    int Col,
    int Length,
    IReadOnlyList<KrizaljkaCell> Cells);
