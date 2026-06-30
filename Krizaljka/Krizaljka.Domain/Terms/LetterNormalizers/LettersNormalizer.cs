
namespace Krizaljka.Domain.Terms.LetterNormalizers;

public static class LettersNormalizer
{
    private const string Dz = "!";
    private const string Lj = "@";
    private const string Nj = "#";

    public static string NormalizeTerm(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return string.Empty;

        var value = term
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "");

        return value
            .Replace("dž", Dz)
            .Replace("lj", Lj)
            .Replace("nj", Nj)
            .Replace("DŽ", Dz)
            .Replace("LJ", Lj)
            .Replace("NJ", Nj);
    }

    public static string NormalizeLetter(string? letter)
    {
        if (string.IsNullOrWhiteSpace(letter))
            return string.Empty;

        return letter.Trim().ToLowerInvariant() switch
        {
            "dž" => Dz,
            "lj" => Lj,
            "nj" => Nj,
            _ => letter.Trim().ToLowerInvariant()
        };
    }
}
