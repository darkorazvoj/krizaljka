using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.WordsConverters;

namespace Krizaljka.Domain.Terms;

public class StructureTermService
{
    public static ITerm Invoke(string term)
    {
        if (string.IsNullOrWhiteSpace(term) ||
            !term.IsValidRawTermValue())
        {
            return new InvalidTerm("empty_or_containing_invalid_characters");
        }

        var termTrimmed = term.TrimExtra();
        if (termTrimmed.Length <= 0)
        {
            return new InvalidTerm($"Term seems to be empty. Term: {termTrimmed}");
        }

        var letters = CroatianWordConverter.GetLetters(termTrimmed);

        List<int> spaceIndexes = [];
        List<int> dashIndexes = [];

        for (var i = 0; i < letters.Count; i++)
        {
            var c = letters[i].ToCharArray();

            if (c is [' '])
            {
                spaceIndexes.Add(i);
            }

            if (c is ['-'])
            {
                dashIndexes.Add(i);
            }
        }

        var lettersDense = letters
            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "-")
            .ToList();

        var denseValue = termTrimmed.GetDenseTerm();

        return new TermComputed(
            termTrimmed.ToUpperInvariant(),
            denseValue.ToUpperInvariant(),
            lettersDense,
            spaceIndexes,
            dashIndexes,
            letters.Count);
    }
}
