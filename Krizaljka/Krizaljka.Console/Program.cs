

using Krizaljka.Console;
using Krizaljka.Domain.KrizaljkaSolved;
using Krizaljka.Domain.Template;
using Krizaljka.Domain.TemplateAnalysis;
using System.Text;
using System.Text.Json;
using Krizaljka.Domain.Terms;
using Krizaljka.Domain.WordsConverters;

Console.OutputEncoding = Encoding.UTF8;


const string pojmoviPath = @"C:\git\krizaljka\pojmovi";
const string dbPath = @"C:\git\krizaljka\pojmovi\db";
const string pojmoviDbName = "pojmovi.json";
const string categoriesDbName = "kategorije.json";

if (args.Length > 0 && args[0] == "d")
{
    var termsLoader = new TermsLoader();
    await termsLoader.LoadTermsAsync(pojmoviPath);

    if (!Directory.Exists(dbPath))
    {
        Directory.CreateDirectory(dbPath);
    }

    List<CategoryJsonDbItem> categories = [];
    foreach (var kv in InMemoryDatabase.CategoriesDb)
    {
        categories.Add(new CategoryJsonDbItem(kv.Key, kv.Value));
    }

    var categoryDb = new CategoryJsonDb(categories);

    // Formatting options (Optional: Makes the JSON pretty/readable)
    var options = new JsonSerializerOptions { WriteIndented = true };

    var categoriesDbJson = JsonSerializer.Serialize(categoryDb, options);
    File.WriteAllText( Path.Combine(dbPath, categoriesDbName), categoriesDbJson);

    List<IValidTerm> validTerms = [];
    foreach (var termsDbValue in InMemoryDatabase.TermsDb.Values)
    {
        foreach (var validTerm in termsDbValue)
        {
            validTerms.Add(validTerm);
        }
    }

    var pojmoviDbJson = JsonSerializer.Serialize(new PojmoviJsonDb(validTerms), options);
    File.WriteAllText(Path.Combine(dbPath, pojmoviDbName), pojmoviDbJson);

    Console.WriteLine("Database Rebuilt!");
    return;
}


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
    //var termsLoader = new TermsLoader();
    //await termsLoader.LoadTermsAsync(@"C:\git\krizaljka\pojmovi");
    //Console.WriteLine($"Number of categories: {InMemoryDatabase.CategoriesDb.Count}");
    //Console.WriteLine($"Number of loaded terms: {InMemoryDatabase.TermsDb.Count}");

    //Console.WriteLine("Number of terms per length:");
    //foreach (var kv in InMemoryDatabase.LengthTermsDb)
    //{
    //    Console.WriteLine($"{kv.Key}: {kv.Value.Count}");
    //}


    KrizaljkaAnalyzer krizaljkaAnalyzer = new();
    var templateAnalysis = krizaljkaAnalyzer.GeTemplateAnalysis(workingTemplate);


    List<AssignedTerm> assignedSlotTerms = [
        new(26, 1,  CroatianWordConverter.GetJustLetters("interferencija".ToUpper())),
        new(12, 1,  CroatianWordConverter.GetJustLetters("PROJEKTANTICASTANA".ToUpper())),
        new(78, 1, CroatianWordConverter.GetJustLetters("dramatičari".ToUpper())),
        new(53, 1, CroatianWordConverter.GetJustLetters("slastičarnice".ToUpper()))
    ];


    StringBuilder sb = new();
    var krizaljka = workingTemplate.Rows;
    for (var r = 0; r < krizaljka.Length; r++)
    {
        for (var c = 0; c < krizaljka[r].Length; c++)
        {
            //sb.Append(krizaljka[r][c]);
            sb.Append($"{GetCellCharacter(krizaljka[r][c]),-7}");

            string GetCellCharacter(int i)
            {
                switch (i)
                {
                    case 0:
                        return "\u25CF";
                    case 1:
                    case 2:
                    case 3:
                        //return "\u25A0";
                        return GetSlotIds(r, c);
                    default:
                        return GetInputValue(r,c);
                }
            }

            sb.Append("    ");
        }

        sb
            .AppendLine()
            .AppendLine();

    }

    Console.WriteLine(sb.ToString());

    string GetSlotIds(int rr, int cc)
    {
        var slotIdsWithDirection = templateAnalysis.Slots
            .Where(x => x.Row == rr && x.Col == cc)
            .Select(x => new { x.Id, x.Direction })
            .ToList();

        StringBuilder sb1 = new();


        foreach (var idDirection in slotIdsWithDirection)
        {
            sb1.Append(idDirection.Id)
                .Append("")
                .Append(idDirection.Direction == KrizaljkaDirection.Right ? ">" : "V")
                .Append("/");
        }

        if (sb1.Length > 0)
        {
            sb1.Remove(sb1.Length - 1, 1);
        }

        return sb1.ToString();
    }

    string GetInputValue(int ir, int ic)
    {
        if (templateAnalysis.CellSlots.TryGetValue((ir, ic), out var slotUsages))
        {
            foreach (var slot in slotUsages)
            {

                var assignedSlot = assignedSlotTerms.FirstOrDefault(x => x.SlotId == slot.SlotId);
                if (assignedSlot is not null)
                {
                    return assignedSlot.Letters[slot.CharIndex];
                }
            }

            //if (slotUsages.Count > 0)
            //{
            //    var slotId = slotUsages[0].SlotId;

            //    var assignedSlot = assignedSlotTerms.FirstOrDefault(x => x.SlotId == slotId);
            //    if (assignedSlot is not null)
            //    {
            //        return assignedSlot.RawValue[slotUsages[0].CharIndex].ToString();
            //    }
            //}
        }

        return "-";
    }
}



//// LOAD TERMS

//var termsLoader = new TermsLoader();

//await termsLoader.LoadTermsAsync(@"C:\git\krizaljka\pojmovi");

//Console.WriteLine($"Number of categories: {InMemoryDatabase.CategoriesDb.Count}");
//Console.WriteLine($"Number of loaded terms: {InMemoryDatabase.TermsDb.Count}");
//Console.WriteLine("Number of terms per length:");

//foreach (var kv in InMemoryDatabase.LengthTermsDb)
//{
//    Console.WriteLine($"{kv.Key}: {kv.RawValue.Count}");
//}


//if (InMemoryDatabase.LengthTermsDb.TryGetValue(13, out var listByLength))
//{
//    Console.WriteLine();
//    foreach (var term in listByLength)
//    {
//        Console.WriteLine(string.Join(',', term.RawValue));
//    }
//}





Console.WriteLine("THE END");
