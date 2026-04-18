
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Caches;

public static class GlobalCaches
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
    public static IReadOnlyDictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>>
        TermsByLengthPositionLetter { get; set; }
        = new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>>();
}
