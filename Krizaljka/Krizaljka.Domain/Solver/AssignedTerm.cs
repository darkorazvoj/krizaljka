
namespace Krizaljka.Domain.Solver;

public record AssignedTerm(int SlotId, long TermId, IReadOnlyList<string> Letters);
