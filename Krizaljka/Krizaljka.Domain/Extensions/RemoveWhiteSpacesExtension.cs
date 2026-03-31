using System.Text;

namespace Krizaljka.Domain.Extensions;

public static class RemoveWhiteSpacesExtension
{
    public static string RemoveWhiteSpaces(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (!char.IsWhiteSpace(c))
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public static string TrimExtra(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
