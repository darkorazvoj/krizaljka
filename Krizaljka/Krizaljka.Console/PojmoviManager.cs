
using System.Text.Json;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Console;

public static class PojmoviManager
{
    private const string DbPath = @"C:\git\krizaljka\pojmovi\db";
    private const string PojmoviDbFileNamePrefix = "pojmovi";
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static PojmoviJsonDb LoadTerms()
    {
        var existingDbFiles = Directory
            .GetFiles(DbPath)
            .Where(x => Path.GetFileNameWithoutExtension(x).StartsWith(PojmoviDbFileNamePrefix))
            .ToList();

        List<Term> terms = [];
        foreach (var dbFile in existingDbFiles)
        {
            var pojmoviDbJson = File.ReadAllText(dbFile);
            if (string.IsNullOrWhiteSpace(pojmoviDbJson))
            {
                continue;
            }

            var pojmoviDb = JsonSerializer.Deserialize<PojmoviJsonDb>(pojmoviDbJson, Options);
            if (pojmoviDb is null)
            {
                continue;
            }
            terms.AddRange(pojmoviDb.Terms);
        }

        return new PojmoviJsonDb(terms);

    }
}
