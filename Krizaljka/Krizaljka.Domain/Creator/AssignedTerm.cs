
namespace Krizaljka.Domain.Creator;

public record AssignedTerm(int SlotId, long TermId, IReadOnlyList<string> Letters);
