using Krizaljka.Console;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.TemplateAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Krizaljka.Domain;
using Krizaljka.Domain.Caches;
using Krizaljka.Domain.Creator;
using Krizaljka.Domain.Template;
using Krizaljka.Domain.Terms;

Console.OutputEncoding = Encoding.UTF8;


const string templatesStatesDir = @"C:\git\krizaljka\templates\states";

TheKrizaljka? theKrizaljka = null;


var pojmoviDb = PojmoviManager.LoadTerms();
var templatesDb = await KrizaljkaTemplatesManager.LoadTemplatesAsync();


var options = new JsonSerializerOptions
    { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),PropertyNameCaseInsensitive = true };

var sbMainMenu = new StringBuilder();
var mainMenu = sbMainMenu.AppendLine("Where?")
    .AppendLine("d -> Create database")
    .AppendLine("l -> lookup words")
    .AppendLine("at -> Add term")
    .AppendLine("td -> Create/Update templates database")
    .AppendLine("kts -> show krizaljka templates list")
    .AppendLine("wl -> Show words per length")
    .AppendLine("lk -> load krizaljka template")
    .AppendLine("k -> Show current krizaljka")
    .AppendLine("kss -> Load solved state for current krizaljka")
    .AppendLine("kp -> Assign pojam to krizaljka")
    .AppendLine("kd -> Delete pojam from krizaljka")
    .AppendLine("kcr -> Run krizaljka creator")
    .AppendLine("kmts -> Run krizaljka template finder and creator for theme words")
    .AppendLine("st => Check processed templates")
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

                Console.Write("Search type (default -> no search term, sw -> starts with, let -> by letters):");
                var searchType = Console.ReadLine();

                var query = pojmoviDb.Terms
                    .Where(x => x.Length == length);

                if (!string.IsNullOrWhiteSpace(searchType))
                {
                    if (searchType.ToUpper() == "SW")
                    {
                        while (true)
                        {
                            Console.Write("Starts with: ");
                            var startsWith = Console.ReadLine();

                            if (string.IsNullOrWhiteSpace(startsWith))
                            {
                                continue;
                            }
                            query = query.Where(x => x.DenseValue.StartsWith(startsWith.RemoveWhiteSpaces(), StringComparison.InvariantCultureIgnoreCase));
                            break;
                        }
                    }
                    else if (searchType.ToUpper() == "LET")
                    {
                        query = query.Where(x => x.Length == length);

                        for (var i = 0; i < length; i++)
                        {
                            var letterIndex = i;
                            Console.Write($"Letter {i+1}: ");
                            var let = Console.ReadLine();

                            if (!string.IsNullOrWhiteSpace(let))
                            {
                                query = query.Where(x => x.Letters[letterIndex].ToUpper() == let.ToUpper());
                            }
                        }
                    }
                }

                var result = query.OrderBy(x => x.DenseValue).ToList();
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
                    $"ID: {template.Id}, {template.Matrix.Length}x{(template.Matrix.Length > 0 ? template.Matrix[0].Length:0)}");
            }
            Console.ReadKey();
            break;

        case "lk":
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
                    continue;
                }

                var template = templatesDb.Templates.FirstOrDefault(x => x.Id == krizaljkaTemplateId);
                if (template is null)
                {
                    Console.WriteLine($"No template with ID {krizaljkaTemplateId}");
                    Console.ReadKey();
                    continue;
                }
                KrizaljkaSolveState? existingState = null;

                // Load state if exists.
                if (Directory.Exists(templatesStatesDir))
                {
                    var templateName = Directory
                        .GetFiles(templatesStatesDir)
                        .Where(x => string.Equals(Path.GetFileName(x), GetTemplateStateFileName(template.Id), StringComparison.CurrentCultureIgnoreCase))
                        .Select(Path.GetFullPath)
                        .FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(templateName))
                    {
                        try
                        {
                            var templateJson = await File.ReadAllTextAsync(templateName);
                            if (string.IsNullOrWhiteSpace(templateJson))
                            {
                                continue;
                            }

                            existingState = JsonSerializer.Deserialize<KrizaljkaSolveState>(templateJson, options);
                        }
                        catch
                        {
                            Console.WriteLine("State failed to load...");
                        }
                    }
                }
                
                theKrizaljka = TheKrizaljka.Create(template, existingState ?? new KrizaljkaSolveState());
                PrintKrizaljka();
                Console.ReadKey();

                break;
            }

            break;

        case "kss":
            if (theKrizaljka is null)
            {
                Console.WriteLine("No krizaljka loaded...");
                Console.ReadKey();
                continue;
            }
            var solvedStates = KrizaljkaStateManager.GetSolvedStates(theKrizaljka.Template.Id);
            if (solvedStates.Count == 0)
            {
                Console.WriteLine("No solved states :(");
                Console.ReadKey();
                continue;
            }

            foreach (var kvp in solvedStates)
            {
                Console.WriteLine($"{kvp.Key} - {kvp.Value.Date}");
            }

            Console.WriteLine();
            while (true)
            {
                Console.Write("Select solved state (x for exit): ");
                var selectedSolvedStateString = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(selectedSolvedStateString))
                {
                    continue;
                }

                if (selectedSolvedStateString.ToUpper() == "X")
                {
                    break;
                }

                if (!int.TryParse(selectedSolvedStateString, out var selectedSolvedState))
                {
                    continue;
                }

                if (solvedStates.TryGetValue(selectedSolvedState, out var state))
                {
                    theKrizaljka = TheKrizaljka.Create(theKrizaljka.Template, state.State);
                    PrintKrizaljka();
                    Console.ReadKey();
                    break;
                }
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

            GlobalCaches.Initialize(pojmoviDb.Terms);

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
                    if (!Directory.Exists(templatesStatesDir))
                    {
                        Directory.CreateDirectory(templatesStatesDir);
                    }

                    var currentStateToWriteJson = JsonSerializer.Serialize(theKrizaljka.State, options);
                    File.WriteAllText(
                        Path.Combine(templatesStatesDir, GetTemplateStateFileName(theKrizaljka.Template.Id)),
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
                    var currentStateToWriteJson = JsonSerializer.Serialize(theKrizaljka.State, options);
                    File.WriteAllText(Path.Combine(templatesStatesDir, GetTemplateStateFileName(theKrizaljka.Template.Id)),
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

            GlobalCaches.Initialize(pojmoviDb.Terms);

            Console.WriteLine($"Started: {DateTime.Now}");
            var createResult = new KrizaljkaCreator(theKrizaljka)
                .TrySolve( 3, CancellationToken.None);

            var ts = TimeSpan.FromMilliseconds(createResult.Stats.ElapsedMilliseconds);
            var elapsed = $"{ts.Hours}h {ts.Minutes}m {ts.Seconds}s";

            Console.WriteLine($"Timed out?: {(createResult.Stats.TimedOut ? "YEP" : "no")}");
            Console.WriteLine($"RecursiveCalls: {createResult.Stats.RecursiveCalls}");
            Console.WriteLine($"CandidateTries: {createResult.Stats.CandidateTries}");
            Console.WriteLine($"Backtracks: {createResult.Stats.Backtracks}");
            Console.WriteLine($"DeadEnds: {createResult.Stats.DeadEnds}");
            Console.WriteLine($"FullyFilledAutoAssignments: {createResult.Stats.FullyFilledAutoAssignments}");
            Console.WriteLine($"SingletonAutoAssignments: {createResult.Stats.SingletonAutoAssignments}");
            Console.WriteLine($"MaxAssignedSlotsReached: {createResult.Stats.MaxAssignedSlotsReached}");
            Console.WriteLine($"FinalAssignedSlots: {createResult.Stats.FinalAssignedSlots}");

            Console.WriteLine();
            Console.WriteLine($"Total Time: {elapsed}");

            if (!createResult.IsCreated)
            {
                Console.WriteLine("No solution found");
                theKrizaljka.ReplaceState(createResult.BestState);
                PrintKrizaljka();
                Console.ReadKey();
                continue;
            }

            // TODO save state
            //KrizaljkaStateManager.SaveSolvedState(createResult.State, theKrizaljka.Template.Id);
            Console.WriteLine("SOLVED!!!!");
            PrintKrizaljka();
            Console.ReadKey();
            break;

        case "kmts":
            if (pojmoviDb?.Terms is null)
            {
                Console.WriteLine("Terms database doesn't exist");
                Console.ReadKey();
                return;
            }
            List<KrizaljkaTemplate> templates = [];
            List<Term> themeTerms = [];
            while (true)
            {
                Console.Clear();
                Console.Write("(A)ll templates or (s)elected? (x for exit): ");
                var templateSelection = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(templateSelection))
                {
                    continue;
                }

                var templateSelectionUpper = templateSelection.ToUpper();

               
                if (templateSelectionUpper == "A")
                {
                    templates.AddRange( templatesDb.Templates);
                }
                else if (templateSelectionUpper == "S")
                {
                    while (true)
                    {
                        Console.Write("Template ID (d for DONE): ");
                        var templateIdString = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(templateIdString))
                        {
                            continue;
                        }

                        var templateIdUpper = templateIdString.ToUpper();
                        if (templateIdUpper == "D")
                        {
                            break;
                        } 
                        
                        if (int.TryParse(templateIdUpper, out var templateId))
                        {
                            var template = templatesDb.Templates.FirstOrDefault(x => x.Id == templateId);
                            if (template is null)
                            {
                                continue;
                            }

                            templates.Add(template);
                        }
                    }
                }
               

                while (true)
                {
                    Console.WriteLine("Term IDs (id1, id2, id2...)");
                    var termIdListString = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(termIdListString))
                    {
                        continue;
                    }

                    var termIdList = termIdListString.Split(',', StringSplitOptions.TrimEntries);
                    foreach (var termIdString in termIdList)
                    {
                        if (!int.TryParse(termIdString, out var termId))
                        {
                            continue;
                        }
                        
                        var term = pojmoviDb.Terms.FirstOrDefault(x => x.Id == termId);
                        if (term != null)
                        {
                            themeTerms.Add(term);
                        }
                    }

                    break;
                }

                break;
            }

            Console.WriteLine($"Number of selected templates: {templates.Count}");
            Console.WriteLine("Theme terms:");
            foreach (var themeTerm in themeTerms)
            {
                Console.WriteLine($"ID: {themeTerm.Id}, '{themeTerm.DenseValue}', ('{themeTerm.DenseValue}') ");
            }

            Console.WriteLine("START or x for exit");
            var startCommand = Console.ReadLine();
            if (startCommand?.ToUpper() == "X")
            {
                continue;
            }

            GlobalCaches.Initialize(pojmoviDb.Terms);

            Console.WriteLine($"Started at {DateTime.Now}...");
            var processId = new KrizaljkaVersionASolver(2).QueueSolveAttempt(
                new KrizaljkaVersionARequest(
                    templates,
                    themeTerms.Select(x => x.Id)
                        .ToList(),
                    20,
                    20,
                    12,
                    10,
                    5,
                    5));

            if (!processId.HasValue)
            {
                Console.WriteLine("ERROR....");
                Console.ReadKey();
                continue;
            }

            Console.WriteLine($"Process ID: {processId}");
            Console.ReadKey();
            

            //if (solved)
            //{
            //    Console.WriteLine();
            //    Console.WriteLine("SOLVED");
            //}

            //if (createResult1 is not null)
            //{
            //    var ts1 = TimeSpan.FromMilliseconds(createResult1.Stats.ElapsedMilliseconds);
            //    var elapsed1 = $"{ts1.Hours}h {ts1.Minutes}m {ts1.Seconds}s";

            //    Console.WriteLine($"Timed out?: {(createResult1.Stats.TimedOut ? "YEP" : "no")}");
            //    Console.WriteLine($"RecursiveCalls: {createResult1.Stats.RecursiveCalls}");
            //    Console.WriteLine($"CandidateTries: {createResult1.Stats.CandidateTries}");
            //    Console.WriteLine($"Backtracks: {createResult1.Stats.Backtracks}");
            //    Console.WriteLine($"DeadEnds: {createResult1.Stats.DeadEnds}");
            //    Console.WriteLine($"FullyFilledAutoAssignments: {createResult1.Stats.FullyFilledAutoAssignments}");
            //    Console.WriteLine($"SingletonAutoAssignments: {createResult1.Stats.SingletonAutoAssignments}");
            //    Console.WriteLine($"MaxAssignedSlotsReached: {createResult1.Stats.MaxAssignedSlotsReached}");
            //    Console.WriteLine($"FinalAssignedSlots: {createResult1.Stats.FinalAssignedSlots}");

            //    Console.WriteLine();
            //    Console.WriteLine($"Total Time: {elapsed1}");
            //}
            //else
            //{
            //    Console.WriteLine("Unknown STATS...");
            //    Console.WriteLine();
            //}

            //if (krizaljkaTemplate is not null)
            //{
            //    var template = templatesDb.Templates.FirstOrDefault(t => t.Id == krizaljkaTemplate.Id);
            //    if (template is null)
            //    {
            //        Console.WriteLine("Template disappeared");
            //        Console.ReadKey();
            //        continue;
            //    }

            //    if (createResult1 is null)
            //    {
            //        Console.WriteLine("CreateResult disappeared");
            //        Console.ReadKey();
            //        continue;
            //    }

            //    if (solved)
            //    {
            //        var solvedState = createResult1?.State;
            //        if (solvedState is null)
            //        {
            //            Console.WriteLine("Solved state disappeared...");
            //            Console.ReadKey();
            //            continue;
            //        }

            //        theKrizaljka = TheKrizaljka.Create(template, solvedState);
            //    }
            //    else
            //    {
            //        var bestState = createResult1.BestState;
            //        theKrizaljka = TheKrizaljka.Create(template, bestState);
            //    }

            //    PrintKrizaljka();
            //}

            //foreach (var themePlacement in krizaljkaThemePlacements)
            //{
            //    var term = pojmoviDb.Terms.FirstOrDefault(t => t.Id == themePlacement.TermId);
            //    var termText = term is null ? string.Empty : $"o: {term.Description}, w: {term.DenseValue}";
            //    Console.Write($"SlotId: {themePlacement.SlotId}, term: ID: {themePlacement.TermId} ({termText})");
            //}

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

static string GetTemplateStateFileName(long templateId) => $"template_{templateId}_state.json";


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
    var krizaljkaRows = theKrizaljka.Template.Matrix;
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
