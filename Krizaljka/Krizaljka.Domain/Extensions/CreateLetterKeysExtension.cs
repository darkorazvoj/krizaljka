
namespace Krizaljka.Domain.Extensions;

public static class CreateLetterKeysExtension
{
    public static string CreateLettersKey(this IReadOnlyList<string> list)
    {
        return string.Join("|", list);
    }
}
