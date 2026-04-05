using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Solver;

public sealed class CandidateRemoval
{
    public Dictionary<int, List<Term>> Removed { get; } = [];
}
