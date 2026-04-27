using System.Text;

namespace Krizaljka.Domain.Core.Stuff.Extensions;

public static class Base64StringExtensions
{
    public static string ToBase64(this string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    public static string ConvertFromBase64StringSafe(this string base64String)
    {
        /*  1. Add padding if needed.
            2. Remove illegal characters.
        * https://gist.github.com/catwell/3046205 */
        var remainder = base64String.Length % 4;
        switch (remainder)
        {
            case 2:
                base64String += "==";
                break;
            case 3:
                base64String += "=";
                break;
        }

        base64String = base64String.Replace("-", "+").Replace("_", "/");

        return Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
    }
}
