

using Krizaljka.Console;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.Template;
using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Krizaljka.Domain.Solver;

Console.OutputEncoding = Encoding.UTF8;


const string pojmoviPath = @"C:\git\krizaljka\pojmovi";
const string dbPath = @"C:\git\krizaljka\pojmovi\db";
const string pojmoviDbName = "pojmovi.json";
const string categoriesDbName = "kategorije.json";

KrizaljkaSolveState krizaljkaState = new();

var sbMainMenu = new StringBuilder();
var mainMenu = sbMainMenu.AppendLine("Where?")
    .AppendLine("d -> Create database")
    .AppendLine("l -> lookup words")
    .AppendLine("lk -> load krizaljka template")
    .AppendLine("k -> Show current krizaljka")
    .AppendLine("kp -> Assign pojam to krizaljka")
    .AppendLine("ks -> Run krizaljka solver")
    .ToString();


while (true)
{
    Console.Clear();
    

    Console.WriteLine(mainMenu);
    var where = Console.ReadLine();

    if (where == "x")
    {
        break;
    }

    switch (where)
    {
        case "d":
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
            var options1 = new JsonSerializerOptions
                { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), };

            var categoriesDbJson = JsonSerializer.Serialize(categoryDb, options1);
            File.WriteAllText(Path.Combine(dbPath, categoriesDbName), categoriesDbJson);

            List<Term> validTerms = [];
            foreach (var termsDbValue in InMemoryDatabase.TermsDb.Values)
            {
                foreach (var validTerm in termsDbValue)
                {
                    validTerms.Add((Term)validTerm);
                }
            }

            var pojmoviDbJsonToWrite = JsonSerializer.Serialize(new PojmoviJsonDb(validTerms), options1);
            File.WriteAllText(Path.Combine(dbPath, pojmoviDbName), pojmoviDbJsonToWrite);

            Console.WriteLine("Database Rebuilt!");
            Console.WriteLine("continue...");
            Console.ReadKey();
            break;

        case "l":
            var pojmoviDb = TermsManager.LoadTerms();
            if (pojmoviDb is null)
            {
                Console.WriteLine("Pojmovi db is null");
                return;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("TERMS LOOKUP");
                Console.Write("Length (x for exit): ");
                var lengthString = Console.ReadLine();

                if (lengthString == "x")
                {
                    break;
                }

                if (!int.TryParse(lengthString, out var length))
                {
                    continue;
                }

                Console.Write("Search term (x for exit): ");
                var searchTerm = Console.ReadLine();

                if (searchTerm == "x")
                {
                    break;
                }

                var denseSearchTerm =
                    string.IsNullOrWhiteSpace(searchTerm)
                        ? searchTerm
                        : searchTerm.RemoveWhiteSpaces().ToUpperInvariant();

                var query = pojmoviDb.Terms
                    .Where(x => x.Length == length);

                if (!string.IsNullOrWhiteSpace(denseSearchTerm))
                {
                    query = query.Where(x => x.DenseValue.Contains(denseSearchTerm));
                }

                var result = query.ToList();
                var termsLoopCts = new CancellationTokenSource();
                Console.WriteLine("Pojmovi (x for cancellation):");

                var termsLoopTask = Task.Run(() => { ListLookupTerms(result, termsLoopCts.Token); });

                void ListLookupTerms(List<Term>? filteredTerms, CancellationToken listingFilteredTermsCt)
                {
                    foreach (var validTerm in filteredTerms ?? [])
                    {
                        if (listingFilteredTermsCt.IsCancellationRequested)
                        {
                            break;
                        }
                        Console.WriteLine($"{validTerm.Id} - {validTerm.RawValue}");
                    }
                }

                await termsLoopTask;

                Console.Write("E(x)it or continue: ");
                var anykey = Console.ReadLine();
                if (anykey == "x")
                {
                    break;
                }
            }

            break;

        case "lk":


            break;
        case "k":

            break;

            default:
            const string templatesDir = @"C:\git\krizaljka\templates";
            var templateNames = Directory.GetFiles(templatesDir).Select(Path.GetFullPath).ToList();

            List<KrizaljkaTemplate> templates = [];

            foreach (var templateName in templateNames)
            {
                try
                {
                    var templateJson = await File.ReadAllTextAsync(templateName);
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

            var workingTemplate = templates.FirstOrDefault(x => x.Id == 2);

            if (workingTemplate is not null)
            {
                var termsDb = TermsManager.LoadTerms();
                if (termsDb is null)
                {
                    Console.WriteLine("Pojmovi db is null");
                    return;
                }


                KrizaljkaAnalyzer krizaljkaAnalyzer = new();
                var templateAnalysis = krizaljkaAnalyzer.GeTemplateAnalysis(workingTemplate);
//                KrizaljkaSolveState krizaljkaState = new();


                if (where == "k" || where == "ks")
                {




                    //if (!KrizaljkaSolver.TryPlaceAssignedTerm(
                    //    templateAnalysis,
                    //    termsDb.Terms,
                    //    11,
                    //    4,
                    //    krizaljkaState,
                    //     out var error1))
                    //{
                    //    Console.WriteLine($"1 - {error1}");
                    //    Console.ReadKey();
                        
                    //}


                    //if (!KrizaljkaSolver.TryPlaceAssignedTerm(
                    //       templateAnalysis,
                    //       termsDb.Terms,
                    //       57,
                    //       3197,
                    //       krizaljkaState,
                    //       out var error2))
                    //{
                    //    Console.WriteLine($"2 - {error2}");
                    //    Console.ReadKey();
                        
                    //}

                    //if (!KrizaljkaSolver.TryPlaceAssignedTerm(
                    //        templateAnalysis,
                    //        termsDb.Terms,
                    //        29,
                    //        746,
                    //        krizaljkaState,
                    //        out var error3))
                    //{
                    //    Console.WriteLine($"3 - {error3}");
                    //    Console.ReadKey();
                        
                    //}


                    //if (!KrizaljkaSolver.TryPlaceAssignedTerm(
                    //        templateAnalysis,
                    //        termsDb.Terms,
                    //        37,
                    //        4814,
                    //        krizaljkaState,
                    //        out var error4))
                    //{
                    //    Console.WriteLine($"4 - {error4}");
                    //    Console.ReadKey();
                        
                    //}


                    if (where == "ks")
                    {
                        Console.WriteLine("Solving...");
                        var solved = KrizaljkaSolver.TrySolve(templateAnalysis, termsDb.Terms, krizaljkaState);

                        if (!solved)
                        {
                            Console.WriteLine("No solution found");
                            Console.ReadKey();
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("SOLVED!!!!");
                        }
                    }
                }

                var assignedSlotTerms = krizaljkaState.AssignedTermsBySlotId.Values.ToList();

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
                                    return GetInputValue(r, c);
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
                                return assignedSlot.Letters[slot.CharIndex].ToUpperInvariant();
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




            Console.WriteLine("Continue...");
            Console.ReadKey();
            break;
    }
}

Console.WriteLine("THE END");


