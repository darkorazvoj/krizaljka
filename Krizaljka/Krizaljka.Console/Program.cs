

using System.ComponentModel.Design;
using Krizaljka.Domain.Models;
using System.Text.Json;

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
            Console.WriteLine($"Template json {templateJson}");
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

Console.WriteLine("THE END");
