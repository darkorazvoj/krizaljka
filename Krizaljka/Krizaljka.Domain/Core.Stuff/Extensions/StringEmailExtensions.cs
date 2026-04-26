using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Krizaljka.Domain.Core.Stuff.Extensions;

public static class StringEmailExtensions
{
    private static readonly Regex EmailRegex = new(
        @"^(?=.{6,254}$)[A-Za-z0-9._%+\-]+@[A-Za-z0-9]([A-Za-z0-9\-]*[A-Za-z0-9])?(\.[A-Za-z0-9]([A-Za-z0-9\-]*[A-Za-z0-9])?)+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public static bool IsValidEmailAddress(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
        {
            return false;
        }

        try
        {
            var addr = new MailAddress(normalized);

            if (addr.Address != normalized)
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        return true;
    }
}
