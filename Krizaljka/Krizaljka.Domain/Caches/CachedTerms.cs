using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Caches;

public static class CachedTerms
{
    public static Dictionary<int, IReadOnlyList<Term>> TermsByLength = [];
}
