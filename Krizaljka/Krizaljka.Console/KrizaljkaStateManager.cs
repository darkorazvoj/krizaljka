using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Krizaljka.Domain.Creator;

namespace Krizaljka.Console;

public static class KrizaljkaStateManager
{
    private const string SolvedTemplatesStatesDir = @"C:\git\krizaljka\templates\states\solved";
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),PropertyNameCaseInsensitive = true };

    public static void SaveSolvedState(KrizaljkaSolveState state, long templateId)
    {
        try
        {
            if (!Directory.Exists(SolvedTemplatesStatesDir))
            {
                Directory.CreateDirectory(SolvedTemplatesStatesDir);
            }

            var currentStateToWriteJson = JsonSerializer.Serialize(state, Options);
            File.WriteAllText(
                Path.Combine(SolvedTemplatesStatesDir, $"template_{templateId}_solved_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.json"),
                currentStateToWriteJson);
        }
        catch (Exception e)
        {
            System.Console.WriteLine($"Saving solved state failed. Error: {e.Message} ");
        }
    }
}
