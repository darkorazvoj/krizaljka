

using System.Text;
using System.Text.Json;
using Krizaljka.Console;
using Krizaljka.Domain.Template;
using Krizaljka.Domain.TemplateAnalysis;

Console.OutputEncoding = Encoding.UTF8;




const string templatesDir = @"C:\git\krizaljka\templates";
var templateNames = Directory.GetFiles(templatesDir).Select(Path.GetFullPath).ToList();

List<KrizaljkaTemplate> templates = [];

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

        if (parsedTemplate is not null)
        {
            templates.Add(parsedTemplate);
            break;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
}

Console.WriteLine("Templates:");
foreach (var krizaljkaTemplate in templates)
{
    Console.WriteLine($"Template id: {krizaljkaTemplate.Id}");
    Console.WriteLine($"Template Name: {krizaljkaTemplate.Name}");
    Console.WriteLine($"Template Num of Rows: {krizaljkaTemplate.Rows.Length}");
}

var workingTemplate = templates.FirstOrDefault(x => x.Id == 1);

if (workingTemplate is not null)
{
    StringBuilder sb = new();
    var krizaljka = workingTemplate.Rows;
    for (var r = 0; r < krizaljka.Length; r++)
    {
        for (var c = 0; c < krizaljka[r].Length; c++)
        {
            //sb.Append(krizaljka[r][c]);
            sb.Append(GetCellCharacter(krizaljka[r][c]));

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


    KrizaljkaAnalyzer krizaljkaAnalyzer = new();
    var templateAnalysis = krizaljkaAnalyzer.GeTemplateAnalysis(workingTemplate);

}



//// LOAD TERMS

//var termsLoader = new TermsLoader();

//await termsLoader.LoadTermsAsync(@"C:\git\krizaljka\pojmovi");

//Console.WriteLine($"Number of categories: {InMemoryDatabase.CategoriesDb.Count}");
//Console.WriteLine($"Number of loaded terms: {InMemoryDatabase.TermsDb.Count}");
//Console.WriteLine("Number of terms per length:");

//foreach (var kv in InMemoryDatabase.LengthTermsDb)
//{
//    Console.WriteLine($"{kv.Key}: {kv.Value.Count}");
//}


Console.WriteLine("THE END");
