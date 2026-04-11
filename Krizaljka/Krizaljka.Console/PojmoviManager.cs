
using System.ComponentModel.Design;
using System.Text.Json;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Console;

public static class PojmoviManager
{
    private const string DbPath = @"C:\git\krizaljka\pojmovi\db";
    private const string AddedPojmoviFullPath = @"C:\git\krizaljka\pojmovi\added.json";
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

    public static async Task<bool> AddTermAsync(string description, string termString)
    {
        if (!string.IsNullOrWhiteSpace(description) &&
            description.TrimExtra().Length > 40)
        {
            return false;
        }

        List<TermJson>? addedPojmovi;

        if (!File.Exists(AddedPojmoviFullPath))
        {
            await File.WriteAllTextAsync(AddedPojmoviFullPath, "[]");
        }

        var addedPojmoviJson = await File.ReadAllTextAsync(AddedPojmoviFullPath);
        if (string.IsNullOrWhiteSpace(addedPojmoviJson))
        {
            addedPojmovi = [];
        }
        else
        {
            addedPojmovi = JsonSerializer.Deserialize<List<TermJson>>(addedPojmoviJson, Options) ?? [];
        }

        addedPojmovi.Add(new TermJson(description, termString));

        var addedPojmoviJsonNew = JsonSerializer.Serialize(addedPojmovi, Options);
        await File.WriteAllTextAsync(AddedPojmoviFullPath, addedPojmoviJsonNew);

        return true;
    }
}
