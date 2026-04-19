
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Caches;

public static class GlobalCaches
{
    public static List<Term> Terms = [];

    public static IReadOnlyList<Term> NormalizedTerms = [];

    public static Dictionary<long, Term> NormalizedTermsById = [];

    public static Dictionary<int, IReadOnlyList<Term>> TermsByLength = [];

    public static Dictionary<int, IReadOnlyDictionary<string, IReadOnlyList<long>>> TermIdsByLengthAndLettersKey = [];


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
