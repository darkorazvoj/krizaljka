
namespace Krizaljka.Domain.Solver;

public record PlacementResult(
    IReadOnlyList<(int SlotId, long TermId)> AssignedSlots,
    IReadOnlyList<(int Row, int Col)> NewCells);
