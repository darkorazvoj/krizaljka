
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Caches;

public static class GlobalCaches
{
    public static Dictionary<int, IReadOnlyList<Term>> TermsByLength = [];
}
