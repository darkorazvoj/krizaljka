using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.WordsConverters;

namespace Krizaljka.Domain.Terms;

public class StructureNewTermService
{
    private const int DescriptionMaxLength = 40;

    public static ITerm Invoke(
        TermLanguage language, 
        string description, 
        string term,
        bool isPrivate)
    {
        if (string.IsNullOrWhiteSpace(term) ||
            !term.IsValidRawTermValue())
        {
            return new InvalidTerm("empty_or_containing_invalid_characters");
        }

        var descTrimmed = description.TrimExtra();
        var descCleaned = descTrimmed.Length > DescriptionMaxLength
            ? descTrimmed[..(DescriptionMaxLength - 3)] + "<>!"
            : descTrimmed;

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

        return new NewTerm(
            language,
            descCleaned,
            termTrimmed.ToUpperInvariant(),
            denseValue.ToUpperInvariant(),
            lettersDense,
            spaceIndexes,
            dashIndexes,
            isPrivate);
    }
}
