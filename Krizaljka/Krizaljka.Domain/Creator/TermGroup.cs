using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Creator;

public sealed class TermGroup(Term representative, IReadOnlyList<long> termIds)
{
    public Term Representative { get; } = representative;

    public IReadOnlyList<long> TermIds { get; } = termIds;
}

