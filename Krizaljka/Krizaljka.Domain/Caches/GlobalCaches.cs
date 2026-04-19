
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Caches;

public static class GlobalCaches
{
    private static bool _isInitialized;

    public static IReadOnlyList<Term> Terms { get; private set; } = [];

    public static IReadOnlyList<Term> NormalizedTerms { get; private set; } = [];

    public static Dictionary<long, Term> NormalizedTermsById { get;private set;  } = [];

    public static Dictionary<int, IReadOnlyList<Term>> TermsByLength { get; private set; } = [];

    public static Dictionary<int, IReadOnlyDictionary<string, IReadOnlyList<long>>>
        TermIdsByLengthAndLettersKey { get; private set; } = [];

    /*
     Length
        -> Position
            -> Letter
                -> Terms
     *
     *
     */
    public static IReadOnlyDictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>>
        TermsByLengthPositionLetter { get; private set; }
        = new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>>();

    public static void Initialize(List<Term> terms)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        Terms = terms;
    
        GlobalCaches.NormalizedTerms = GetNormalizedTerms(GlobalCaches.Terms);

        if (GlobalCaches.TermsByLength.Count == 0)
        {
            GlobalCaches.TermsByLength = GlobalCaches.NormalizedTerms
                .GroupBy(x => x.Length)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<Term>)x
                        .OrderBy(t => t.DenseValue, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(t => t.Id)
                        .ToList());
        }

        if (GlobalCaches.NormalizedTermsById.Count == 0)
        {
            GlobalCaches.NormalizedTermsById = GlobalCaches.NormalizedTerms.ToDictionary(x => x.Id);
        }

        if (GlobalCaches.TermIdsByLengthAndLettersKey.Count == 0)
        {
            GlobalCaches.TermIdsByLengthAndLettersKey = GlobalCaches.NormalizedTerms
                .GroupBy(x => x.Length)
                .ToDictionary(
                    lengthGroup => lengthGroup.Key,
                    lengthGroup =>
                        (IReadOnlyDictionary<string, IReadOnlyList<long>>)lengthGroup
                            .GroupBy(term => term.Letters.CreateLettersKey())
                            .ToDictionary(
                                group => group.Key,
                                group => (IReadOnlyList<long>)group
                                    .OrderBy(t => t.Id)
                                    .Select(t => t.Id)
                                    .ToList(),
                                StringComparer.Ordinal));
        }

        if (GlobalCaches.TermsByLengthPositionLetter.Count == 0)
        {
            var result =
                new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>>();

            foreach (var lengthGroup in GlobalCaches.NormalizedTerms.GroupBy(x => x.Length))
            {
                var positionMap = new Dictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>();

                for (var i = 0; i < lengthGroup.Key; i++)
                {
                    var letterMap = lengthGroup
                        .GroupBy(term => term.Letters[i])
                        .ToDictionary(
                            x => x.Key,
                            x => (IReadOnlyList<Term>)x
                                .OrderBy(t => t.DenseValue, StringComparer.OrdinalIgnoreCase)
                                .ThenBy(t => t.Id)
                                .ToList());

                    positionMap.Add(i, letterMap);
                }

                result.Add(lengthGroup.Key, positionMap);
            }

            GlobalCaches.TermsByLengthPositionLetter = result;
        }
    }

    private static IReadOnlyList<Term> GetNormalizedTerms(IReadOnlyList<Term> terms) =>
        terms
            .Select(t => t with
            {
                Letters = t.Letters.Select(x => x.NormalizeLetters()).ToArray()
            })
            .ToList()
            .AsReadOnly();
}
