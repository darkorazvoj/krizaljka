
namespace Krizaljka.Domain.Extensions;

public static class TermStringExtensions
{
    public static bool IsValidRawTermValue(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var hasLetter = false;

        foreach (var c in value)
        {
            if (char.IsLetter(c))
            {
                hasLetter = true;
            }
            else if(c is not ' ' and not '-')
            {
                return false;
            }
        }

        return hasLetter;
    }

    public static string GetDenseTerm(this string term)
    {
        if (!term.IsValidRawTermValue())
        {
            return string.Empty;
        }

        return term
            .RemoveWhiteSpaces()
            .RemoveDashes();
    }
}
