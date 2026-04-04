using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Caches;

public static class CachedTerms
{
    public static Dictionary<int, IReadOnlyList<Term>> TermsByLength = [];

    /*
     Length
        -> Position
            -> Letter
                -> Terms
     *
     * 
     */

    public static IReadOnlyDictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>> TermsByLengthPositionLetter
        = new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>>();
}
