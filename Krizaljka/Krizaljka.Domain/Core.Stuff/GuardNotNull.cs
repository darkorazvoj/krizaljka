using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Krizaljka.Domain.Core.Stuff;

public static class GuardNotNull
{
    /// <summary>
    /// Makes sure the value is not null and helps with compiler warning.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static T Required<T>(
        [NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }

        return value;
    }
}


