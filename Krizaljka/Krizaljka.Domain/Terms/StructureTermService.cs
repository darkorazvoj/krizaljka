using Krizaljka.Domain.Extensions;

namespace Krizaljka.Domain.Terms;

public class StructureTermService
{
    private const int DescriptionMaxLength = 36;

    public ITerm Invoke(string description, string term, int category)
    {
        if (string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(term))
        {
            return new InvalidTerm($"missing description and/or term. Description: {description}, Term: {term}");
        }

        var descCleaned = description.TrimExtra();

        if (descCleaned.Length > 36)
        {
            return new InvalidTerm($"Description > {DescriptionMaxLength}, Length: {descCleaned.Length}, Description: {descCleaned}");
        }

        var termTrimmed = term.TrimExtra();
        if (termTrimmed.Length <= 0)
        {
            return new InvalidTerm($"Term seems to be empty. Term: {termTrimmed}");
        }

        List<int> spaceIndexes = [];
        List<int> dashIndexes = [];
        for (var i = 0; i < termTrimmed.Length; i++)
        {
            var c = termTrimmed[i];

            if (c == ' ')
            {
                spaceIndexes.Add(i);
            }

            if (c == '-')
            {
                dashIndexes.Add(i);
            }
        }

        var termCompressed = termTrimmed.RemoveWhiteSpaces();

        return new Term(
            descCleaned,
            termCompressed,
            termCompressed.Length,
            category,
            spaceIndexes,
            dashIndexes);
    }
}
