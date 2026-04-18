using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Creator;

public sealed record DomainChange(
    int SlotId,
    bool HadValue,
    IReadOnlyList<Term>? PreviousValue);
