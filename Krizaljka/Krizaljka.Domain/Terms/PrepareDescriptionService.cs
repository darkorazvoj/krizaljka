using Krizaljka.Domain.Extensions;

namespace Krizaljka.Domain.Terms;

internal static class PrepareDescriptionService
{
    private const int DescriptionMaxLength = 40;

    public static string? Invoke(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var descTrimmed = description.TrimExtra();
        return descTrimmed.Length > DescriptionMaxLength
            ? descTrimmed[..(DescriptionMaxLength - 3)] + "<>!"
            : descTrimmed;
    }
}
