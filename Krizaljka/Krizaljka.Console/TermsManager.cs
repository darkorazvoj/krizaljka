
using System.Text.Json;

namespace Krizaljka.Console;

public static class TermsManager
{
    private const string DbPath = @"C:\git\krizaljka\pojmovi\db";
    private const string PojmoviDbName = "pojmovi.json";

    public static PojmoviJsonDb? LoadTerms()
    {
        var pojmoviDbJson = File.ReadAllText(Path.Combine(DbPath, PojmoviDbName));
        if (string.IsNullOrWhiteSpace(pojmoviDbJson))
        {
            return null;
        }

        var options2 = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        
        try
        {
            return JsonSerializer.Deserialize<PojmoviJsonDb>(pojmoviDbJson, options2);
        }
        catch (Exception e)
        {
            return null;
        }

    }
}
