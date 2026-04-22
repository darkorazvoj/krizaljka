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
                Path.Combine(SolvedTemplatesStatesDir, $"template_{templateId}_solved_{DateTime.Now.ToString("yyyyMMddTHHmmss")}.json"),
                currentStateToWriteJson);
        }
        catch (Exception e)
        {
            System.Console.WriteLine($"Saving solved state failed. Error: {e.Message} ");
        }
    }

    public static Dictionary<int, (KrizaljkaSolveState State, string Date)> GetSolvedStates(long templateId)
    {
        Dictionary<int, (KrizaljkaSolveState State, string Date)> states = [];

        if (!Directory.Exists(SolvedTemplatesStatesDir))
        {
            return states;
        }

        var dbDirectory = new DirectoryInfo(SolvedTemplatesStatesDir);
        var solvedStatesFiles = dbDirectory.GetFiles($"template_{templateId}_solved_*.json");

        var currentKey = 1;
        foreach (var file in solvedStatesFiles)
        {
            try
            {
                var stateJsonString = File.ReadAllText(file.FullName);
                if (string.IsNullOrWhiteSpace(stateJsonString))
                {
                    continue;
                }

                var state = JsonSerializer.Deserialize<KrizaljkaSolveState>(stateJsonString);
                if (state is not null)
                {
                    var suffix = "solved";
                    var fileNameParts = file.Name.Split('_');
                    if (fileNameParts.Length == 4)
                    {
                        suffix = fileNameParts[3];
                    }

                    states.Add(currentKey, (state, suffix));
                    currentKey++;
                }
            }
            catch
            {
                System.Console.WriteLine($"Invalid solved state file: {file.Name}");
            }
        }

        return states;
    }
}
