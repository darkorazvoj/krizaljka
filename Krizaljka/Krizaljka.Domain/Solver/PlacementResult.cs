
namespace Krizaljka.Domain.Solver;

public record PlacementResult(
    int SlotId,
    long TermId,
    IReadOnlyList<(int Row, int Cell)> NewCells);
