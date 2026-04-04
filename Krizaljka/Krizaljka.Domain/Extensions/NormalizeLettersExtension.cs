
namespace Krizaljka.Domain.Extensions;

public static class NormalizeLettersExtension
{
    public static string NormalizeLetters(this string value) => value.Trim().ToUpperInvariant();
}
