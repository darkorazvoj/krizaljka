

using System.Text;
using Krizaljka.Domain.Models;
using System.Text.Json;
using Krizaljka.Console;

Console.OutputEncoding = Encoding.UTF8;

var templateIdArg = args[0];

if (string.IsNullOrWhiteSpace(templateIdArg))
{
    Console.WriteLine("Missing template Id.");
    return;
}

if (!long.TryParse(templateIdArg, out var templateId))
{
    return;
}

Console.WriteLine($"Template id: {templateId}");

const string templatesDir = @"C:\git\krizaljka\templates";
var templateNames = Directory.GetFiles(templatesDir).Select(Path.GetFullPath).ToList();

KrizaljkaTemplate? template = null;

foreach (var templateName in templateNames)
{
    try
    {
        var templateJson =  await File.ReadAllTextAsync(templateName);
        if (string.IsNullOrWhiteSpace(templateJson))
        {
            continue;
        }
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var parsedTemplate = JsonSerializer.Deserialize<KrizaljkaTemplate>(templateJson, options);

        if (parsedTemplate is not null && parsedTemplate.Id == templateId)
        {
            template = parsedTemplate;
            break;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
}

if (template is null)
{
    Console.WriteLine($"Template with id {templateId} not found. Better luck next time!");
    return;
}

Console.WriteLine($"Template id: {template.Id}");
Console.WriteLine($"Template Name: {template.Name}");
Console.WriteLine($"Template Num of Rows: {template.Rows.Length}");

StringBuilder sb = new();
var krizaljka = template.Rows;
for (var x = 0; x < krizaljka.Length; x++)
{
    for (var y = 0; y < krizaljka[x].Length; y++)
    {
        sb.Append(GetCellCharacter(krizaljka[x][y]));

        string GetCellCharacter(int i)
        {
            switch (i)
            {
                case 0:
                    return "\u25CF";
                case 1:
                case 2:
                case 3:
                    return "\u25A0";
                default:
                    return "-";
            }
        }

        sb.Append("   ");
    }

    sb
        .AppendLine()
        .AppendLine();
}

Console.WriteLine(sb.ToString());

var termsLoader = new TermsLoader();

await termsLoader.LoadTermsAsync(@"C:\git\krizaljka\pojmovi");

Console.WriteLine($"Number of categories: {InMemoryDatabase.CategoriesDb.Count}");
Console.WriteLine($"Number of loaded terms: {InMemoryDatabase.TermsDb.Count}");
Console.WriteLine("Number of terms per length:");

foreach (var kv in InMemoryDatabase.LengthTermsDb)
{
    Console.WriteLine($"{kv.Key}: {kv.Value.Count}");
}


Console.WriteLine("THE END");
