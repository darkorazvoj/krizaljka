

using System.Diagnostics;
using Krizaljka.Console;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.Template;
using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Krizaljka.Domain;
using Krizaljka.Domain.Solver;

Console.OutputEncoding = Encoding.UTF8;


const string pojmoviPath = @"C:\git\krizaljka\pojmovi";
const string dbPath = @"C:\git\krizaljka\pojmovi\db";
const string pojmoviDbName = "pojmovi.json";
const string categoriesDbName = "kategorije.json";
const string templatesDir = @"C:\git\krizaljka\templates";

TheKrizaljka? theKrizaljka = null;

string? currentTemplateName = null;
var termsDb = TermsManager.LoadTerms();

var options1 = new JsonSerializerOptions
    { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), };

var sbMainMenu = new StringBuilder();
var mainMenu = sbMainMenu.AppendLine("Where?")
    .AppendLine("d -> Create database")
    .AppendLine("l -> lookup words")
    .AppendLine("kts -> show krizaljka templates list")
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

        case "kts":
            var templateNamesForShow = Directory.GetFiles(templatesDir).Select(Path.GetFullPath).ToList();
            Console.WriteLine("Template IDs:");
            foreach (var templateName in templateNamesForShow)
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

                    
                    if (parsedTemplate?.Id is not null)
                    {
                        Console.WriteLine(parsedTemplate.Id);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            Console.ReadKey();
            break;

        case "lk":
            //currentKrizaljkaTemplate = null;
            currentTemplateName = null;
            //currentKrizaljkaTemplateAnalysis = null;
            //currentKrizaljkaState = null;
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

                if(!int.TryParse(krizaljkaTemplateIdString, out var krizaljkaTemplateId))
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
                                        JsonSerializer.Deserialize<KrizaljkaSolveState>(currentKrizaljkaStateJson, options1);
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

            if (termsDb is null)
            {
                Console.WriteLine("no terms database");
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
                termsDb.Terms,
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
                    File.WriteAllText(Path.Combine(dbPath, GetTemplateStateFileName(currentTemplateName?? "no_name")), currentStateToWriteJson);
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

            if (theKrizaljka.State.ClearSlot(slotToDelete))
            {
                try
                {
                    var currentStateToWriteJson = JsonSerializer.Serialize(theKrizaljka.State, options1);
                    File.WriteAllText(Path.Combine(dbPath, GetTemplateStateFileName(currentTemplateName?? "no_name")), currentStateToWriteJson);
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

            if (theKrizaljka is null || termsDb is null)
            {
                Console.WriteLine("Krizaljka template or other objects not loaded.");
                Console.ReadKey();
                continue;
            }

            var timer = Stopwatch.StartNew();
            Console.WriteLine($"Started: {DateTime.Now}");
            var createResult = new KrizaljkaCreator(theKrizaljka).TrySolve(termsDb.Terms);

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

            break;
        case "k":
            PrintKrizaljka();
            Console.ReadKey();
            break;

           
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
