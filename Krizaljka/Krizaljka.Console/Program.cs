using System.Diagnostics;
using Krizaljka.Console;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.Template;
using Krizaljka.Domain.TemplateAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Krizaljka.Domain;
using Krizaljka.Domain.Creator;

Console.OutputEncoding = Encoding.UTF8;


const string dbPath = @"C:\git\krizaljka\pojmovi\db";
const string templatesDir = @"C:\git\krizaljka\templates";

TheKrizaljka? theKrizaljka = null;

string? currentTemplateName = null;
var pojmoviDb = PojmoviManager.LoadTerms();
var templatesDb = await KrizaljkaTemplatesManager.LoadTemplatesAsync();


var options1 = new JsonSerializerOptions
    { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), };

var sbMainMenu = new StringBuilder();
var mainMenu = sbMainMenu.AppendLine("Where?")
    .AppendLine("d -> Create database")
    .AppendLine("l -> lookup words")
    .AppendLine("at => Add term")
    .AppendLine("td -> Create/Update templates database")
    .AppendLine("kts -> show krizaljka templates list")
    .AppendLine("wl -> Show words per length")
    .AppendLine("lk -> load krizaljka template")
    .AppendLine("k -> Show current krizaljka")
    .AppendLine("kp -> Assign pojam to krizaljka")
    .AppendLine("kd -> Delete pojam from krizaljka")
    .AppendLine("kcr -> Run krizaljka creator")
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
        case "hrl":
            HrRijeciLoader.Load();
            Console.ReadKey();
            break;

        case "d":
            var numOfTerms = await  RebuildDatabaseAsync();

            Console.WriteLine("Database Rebuilt!");
            Console.WriteLine($"Number of terms: {numOfTerms.NumberOfTerms}");
            Console.WriteLine($"Number of NEW terms: {numOfTerms.NumberOfNewTerms}");
            Console.WriteLine("continue...");
            Console.ReadKey();
            break;

        case "at":
            List<string> addedTerms = [];
            Console.WriteLine("Add Term");
            Console.WriteLine();
            while (true)
            {
                Console.Write("Description (x for exit): ");
                var description = Console.ReadLine();
                if (IsExit(description))
                {
                    break;
                }

                Console.Write("Term (x for exit): ");
                var termText = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(termText))
                {
                    Console.WriteLine("Missing term!");
                    Console.ReadLine();
                    continue;
                }
                if (IsExit(description))
                {
                    break;
                }


                var isAdded = await PojmoviManager.AddTermAsync(description ?? "", termText);
                if (isAdded)
                {
                    addedTerms.Add(termText);
                }
            }

            if (addedTerms.Count > 0)
            {
                var termsCount = await RebuildDatabaseAsync();
                Console.WriteLine($"Number of NEW terms: {termsCount.NumberOfNewTerms}");
                if (pojmoviDb?.Terms is null)
                {
                    Console.WriteLine("DB was not created!");
                    continue;
                }

                foreach (var addedTerm in addedTerms)
                {
                    var t = pojmoviDb.Terms.Where(x => x.RawValue.Contains(addedTerm, StringComparison.CurrentCultureIgnoreCase)).ToList();
                    foreach (var term in t)
                    {
                        Console.WriteLine($"ID: {term.Id} ({term.RawValue})");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("continue...");
            Console.ReadLine();

            break;

            static bool IsExit(string? s) => s is not null && s.ToUpper() == "X";

        case "wl":
            if (pojmoviDb?.Terms is null)
            {
                Console.WriteLine("no database...");
                Console.ReadKey();
                continue;
            }

            var lengthNumOfWords = new SortedDictionary<int, int>();
            foreach (var term in pojmoviDb.Terms)
            {
                if (lengthNumOfWords.TryGetValue(term.Length, out var numOfWords))
                {
                    lengthNumOfWords[term.Length] = numOfWords + 1;
                }
                else
                {
                    lengthNumOfWords.Add(term.Length, 1);
                }
            }

            Console.WriteLine("Word length: Number of words");
            foreach (var lengthNumOfWord in lengthNumOfWords)
            {
                Console.WriteLine($"{lengthNumOfWord.Key}: {lengthNumOfWord.Value}");
            }

            Console.ReadKey();
            break;

        case "l":
            pojmoviDb ??= PojmoviManager.LoadTerms();
            if (pojmoviDb.Terms is null)
            {
                Console.WriteLine("Pojmovi database is empty...");
                Console.ReadKey();
                continue;
            }

            if (pojmoviDb.Terms.Count == 0)
            {
                Console.WriteLine("Pojmovi database is empty...");
                Console.ReadKey();
                continue;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"number of terms: {pojmoviDb.Terms.Count}");

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
                Console.WriteLine($"Number of words: {result.Count}");

                var subCounter = 0;
                const int maxWordsPerPage = 50;

                foreach (var validTerm in result)
                {
                    subCounter++;
                    Console.WriteLine($"{validTerm.Id} - {validTerm.RawValue}, ({validTerm.Description})");

                    if (subCounter >= maxWordsPerPage)
                    {
                        subCounter = 0;
                        Console.WriteLine("ENTER to continue or e(x)it?");
                        var inp = Console.ReadLine();
                        if (inp?.ToUpper() == "X")
                        {
                            break;
                        }
                    }
                }

                Console.Write("E(x)it or continue: ");
                var anykey = Console.ReadLine();
                if (anykey == "x")
                {
                    break;
                }
            }

            break;

        case "td":

            var (numOfExisting, numOfNew) = await KrizaljkaTemplatesManager.CreateTemplateDatabaseAsync();
            templatesDb = await KrizaljkaTemplatesManager.LoadTemplatesAsync();

            Console.WriteLine($"Number of existing templates: {numOfExisting}");
            Console.WriteLine($"Number of new templates: {numOfNew}");

            Console.ReadKey();

            break;

        case "kts":
            Console.WriteLine("Template IDs:");
            foreach (var template in templatesDb.Templates)
            {
                Console.WriteLine(
                    $"ID: {template.Id}, Name: {template.Name}, {template.Rows.Length}x{(template.Rows.Length > 0 ? template.Rows[0].Length:0)}");
            }
            Console.ReadKey();
            break;

        case "lk":
            currentTemplateName = null;
            theKrizaljka = null;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Krizaljka template id (x for exit):");
                var krizaljkaTemplateIdString = Console.ReadLine();
                if (krizaljkaTemplateIdString == "x")
                {
                    break;
                }

                if (!int.TryParse(krizaljkaTemplateIdString, out var krizaljkaTemplateId))
                {
                    break;
                }

                var templateNamesForLoadOne = Directory.GetFiles(templatesDir).Select(Path.GetFullPath).ToList();
                foreach (var templateName in templateNamesForLoadOne)
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

                        if (parsedTemplate?.Id == krizaljkaTemplateId)
                        {
                            currentTemplateName = templateName;
                            var templateStateFileName = GetTemplateStateFileName(currentTemplateName);
                            KrizaljkaSolveState? existingState = null;
                            if (File.Exists(templateStateFileName))
                            {
                                try
                                {
                                    var currentKrizaljkaStateJson = await File.ReadAllTextAsync(templateStateFileName);
                                    existingState =
                                        JsonSerializer.Deserialize<KrizaljkaSolveState>(currentKrizaljkaStateJson,
                                            options1);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }

                            existingState ??= new KrizaljkaSolveState();

                            theKrizaljka = TheKrizaljka.Create(parsedTemplate, existingState);
                            Console.WriteLine("Template loaded...");
                            PrintKrizaljka();
                            Console.ReadKey();

                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("Template loading FAILED!");
                        Console.ReadKey();
                    }
                }

                break;
            }

            break;

        case "kp":
            pojmoviDb ??= PojmoviManager.LoadTerms();
            if (pojmoviDb?.Terms is null)
            {
                Console.WriteLine("Pojmovi database is empty...");
                Console.ReadKey();
                continue;
            }
            
            if (pojmoviDb.Terms.Count == 0)
            {
                Console.WriteLine("Pojmovi database is empty...");
                Console.ReadKey();
                continue;
            }

            if (theKrizaljka is null)
            {
                Console.WriteLine("Krizaljka template not loaded.");
                Console.ReadKey();
                continue;
            }

            var exitKp = false;
            var slotIdInput = 0;
            var termIdInput = 0;

            while (true)
            {
                PrintKrizaljka();
                Console.Write("Slot id (x for exit): ");
                var slotIdInputString = Console.ReadLine();
                if (slotIdInputString == "x")
                {
                    exitKp = true;
                    break;
                }

                if (!int.TryParse(slotIdInputString, out slotIdInput))
                {
                    continue;
                }

                break;
            }

            if (exitKp)
            {
                continue;
            }

            while (true)
            {
                Console.Write("Term id (x for exit): ");
                var termIdInputString = Console.ReadLine();
                if (termIdInputString == "x")
                {
                    exitKp = true;
                    break;
                }

                if (!int.TryParse(termIdInputString, out termIdInput))
                {
                    continue;
                }

                break;
            }


            if (exitKp)
            {
                continue;
            }

            if (slotIdInput <= 0 || termIdInput <= 0)
            {
                Console.WriteLine("Term not assigned to a slot...");
                Console.ReadKey();
                continue;
            }


            if (!new KrizaljkaCreator(theKrizaljka).TryPlaceAssignedTermManually(
                    pojmoviDb.Terms,
                    slotIdInput,
                    termIdInput,
                    out var errorAssigningTermToSlot))
            {
                Console.WriteLine(errorAssigningTermToSlot);
                Console.ReadKey();
            }
            else
            {
                try
                {
                    var currentStateToWriteJson = JsonSerializer.Serialize(theKrizaljka.State, options1);
                    File.WriteAllText(Path.Combine(dbPath, GetTemplateStateFileName(currentTemplateName ?? "no_name")),
                        currentStateToWriteJson);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                PrintKrizaljka();
                Console.WriteLine("Term assigned to slot");
                Console.ReadKey();
            }

            break;

        case "kd":
            if (theKrizaljka is null)
            {
                Console.WriteLine("Krizaljka template not loaded.");
                Console.ReadKey();
                continue;
            }

            var slotToDelete = -1;
            var exitDp = false;
            while (true)
            {
                PrintKrizaljka();

                Console.Write("Slot ID to delete (x for exit): ");
                var slotIdToDeleteString = Console.ReadLine();
                if (slotIdToDeleteString == "x")
                {
                    exitDp = true;
                    break;
                }

                if (!int.TryParse(slotIdToDeleteString, out slotToDelete))
                {
                    continue;
                }

                if (slotToDelete <= 0)
                {
                    continue;
                }

                break;
            }

            if (exitDp)
            {
                continue;
            }

            if (theKrizaljka.ClearSlot(slotToDelete))
            {
                try
                {
                    var currentStateToWriteJson = JsonSerializer.Serialize(theKrizaljka.State, options1);
                    File.WriteAllText(Path.Combine(dbPath, GetTemplateStateFileName(currentTemplateName ?? "no_name")),
                        currentStateToWriteJson);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    continue;
                }

                Console.Clear();
                PrintKrizaljka();
                Console.WriteLine("DELETED");
                Console.ReadKey();

                continue;
            }

            Console.WriteLine("Delete FAILED");
            PrintKrizaljka();
            Console.ReadKey();

            break;
        case "kcr":

            if (theKrizaljka is null)
            {
                Console.WriteLine("Krizaljka template or other objects not loaded.");
                Console.ReadKey();
                continue;
            }

            if (pojmoviDb?.Terms is null)
            {
                Console.WriteLine("Krizaljka template or other objects not loaded.");
                Console.ReadKey();
                continue;
            }

            var timer = Stopwatch.StartNew();
            Console.WriteLine($"Started: {DateTime.Now}");
            var createResult = new KrizaljkaCreator(theKrizaljka).TrySolve(pojmoviDb.Terms);

            timer.Stop();
            var ts = timer.Elapsed;
            var elapsed = $"{ts.Minutes:00}:{ts.Seconds:00}";

            Console.WriteLine($"Total Time: {elapsed}");
            Console.WriteLine($"Word tried: {createResult.WordsTried}");

            if (!createResult.IsCreated)
            {
                Console.WriteLine("No solution found");
                Console.ReadKey();
                continue;
            }

            Console.WriteLine("SOLVED!!!!");
            PrintKrizaljka();
            Console.ReadKey();

            break;
        case "k":
            PrintKrizaljka();
            Console.ReadKey();
            break;
    }

    continue;

    async Task<(long NumberOfTerms, long NumberOfNewTerms)> RebuildDatabaseAsync()
    {
        var numOfTerms = await PojmoviDbCreatorJson.CreateDatabaseAsync();
        pojmoviDb = PojmoviManager.LoadTerms();

        return numOfTerms;
    }
}

Console.WriteLine("THE END");

static string GetTemplateStateFileName(string templateName) => $"{templateName}_state.json";


void PrintKrizaljka()
{
    if (theKrizaljka is null)
    {
        Console.WriteLine("No krizaljka loaded...");
        Console.ReadKey();
        return;
    }

    Console.WriteLine($"Krizaljka template: {theKrizaljka.Template.Id}");

    StringBuilder sb = new();
    var krizaljkaRows = theKrizaljka.Template.Rows;
    for (var r = 0; r < krizaljkaRows.Length; r++)
    {
        for (var c = 0; c < krizaljkaRows[r].Length; c++)
        {
            //sb.Append(krizaljka[r][c]);
            sb.Append($"{GetCellCharacter(krizaljkaRows[r][c]),-7}");

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
        var slotIdsWithDirection = theKrizaljka.Slots
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
        if (theKrizaljka.CellSlots.TryGetValue((ir, ic), out var slotUsages))
        {
            foreach (var slot in slotUsages)
            {

                var assignedSlot = theKrizaljka.AssignedTerms.FirstOrDefault(x => x.SlotId == slot.SlotId);
                if (assignedSlot is not null)
                {
                    return assignedSlot.Letters[slot.CharIndex].ToUpperInvariant();
                }
            }
        }

        return "-";
    }
}
