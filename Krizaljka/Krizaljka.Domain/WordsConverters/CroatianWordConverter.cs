
namespace Krizaljka.Domain.WordsConverters;

public static class CroatianWordConverter
{
    public static IReadOnlyList<string> GetLetters(string value)
    {
        List<string> letters = [];

        for (var i = 0; i < value.Length; i++)
        {
            var current = char.ToUpperInvariant(value[i]);

            if (i + 1 < value.Length)
            {
                var next = char.ToUpperInvariant(value[i + 1]);
                if ((current == 'D' && next == 'Ž') ||
                    (current == 'L' && next == 'J') ||
                    (current == 'N' && next == 'J'))
                {
                    letters.Add(string.Concat(value[i], value[i+1]));
                    i++;
                    continue;
                }
            }
            letters.Add(value[i].ToString());
        }
        return letters.AsReadOnly();
    }

    public static IReadOnlyList<string> GetJustLetters(string value)
    {
        var letters = GetLetters(value);
        return letters.Where(s => !string.IsNullOrWhiteSpace(s) && s != "-")
            .ToList()
            .AsReadOnly();
    }
}
