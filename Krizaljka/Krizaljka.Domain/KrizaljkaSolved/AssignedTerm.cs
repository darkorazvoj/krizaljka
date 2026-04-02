
namespace Krizaljka.Domain.KrizaljkaSolved;

public record AssignedTerm(int SlotId, long TermId, IReadOnlyList<string> Letters);
