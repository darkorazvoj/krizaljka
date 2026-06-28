
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.Terms;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Krizaljka.Console.FileFormatConverters;

public static class OneWordFileToTermJsonConverter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, 
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), 
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };


    public static void Load(string fullPath, string? separator = null, int? termOrdinalNumber = null)
    {
        if (!File.Exists(fullPath))
        {
            System.Console.WriteLine("Di je file?");
            return;
        }

        Func<string, string> extractTerm = !string.IsNullOrWhiteSpace(separator) && termOrdinalNumber.HasValue
            ? termLine => ExtractTerm(termLine, separator, termOrdinalNumber.Value)
            : termLine => termLine;

        List<TermJson> list = [];

        foreach (var line in File.ReadLines(fullPath))
        {
            list.Add(new TermJson("", extractTerm(line.TrimExtra())));
        }

        if (list.Count == 0)
        {
            System.Console.WriteLine("No data to import and convert.");
            return;
        }

        var jsonString = JsonSerializer.Serialize(list, Options);
        File.WriteAllText(Path.ChangeExtension(fullPath, ".json"), jsonString);

        System.Console.WriteLine($"DONE! Num of terms: {list.Count}");
    }
    

    //public static void Load(string fullPath, char separator, int termOrdinalNumber)
    //{
    //    if (!File.Exists(fullPath))
    //    {
    //        System.Console.WriteLine("Di je file?");
    //        return;
    //    }

    //    List<TermJson> list = [];

    //    foreach (var line in File.ReadLines(fullPath))
    //    {
    //        list.Add(new TermJson("", line.TrimExtra()));
    //    }

    //    if (list.Count == 0)
    //    {
    //        System.Console.WriteLine("No data to import and convert.");
    //        return;
    //    }

    //    var jsonString = JsonSerializer.Serialize(list, Options);
    //    File.WriteAllText(Path.ChangeExtension(fullPath, ".json"), jsonString);

    //    System.Console.WriteLine($"DONE! Num of terms: {list.Count}");
    //}

    private static string ExtractTerm(string termLine, string separator, int termOrdinalNumber) =>
        termLine.Split(separator)[termOrdinalNumber]?.TrimExtra() ?? "";
}
