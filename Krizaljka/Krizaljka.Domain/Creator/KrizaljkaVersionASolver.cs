using Krizaljka.Domain.Template;
using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Channels;

namespace Krizaljka.Domain.Creator;

public sealed class KrizaljkaVersionASolver
{
    private const string ProcessedTemplatesStatesDir = @"C:\git\krizaljka\templates\states\processed";
    private static readonly JsonSerializerOptions Options = new()
        { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), PropertyNameCaseInsensitive = true  };
    private readonly Channel<SolveAttemptMessage> _channel;

    public KrizaljkaVersionASolver(int workerCount = 5)
    {
        _channel = Channel.CreateUnbounded<SolveAttemptMessage>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });

        for (var i = 0; i < workerCount; i++)
        {
            _ = Task.Run(ProcessQueueAsync);
        }
    }

    public Guid? QueueSolveAttempt(KrizaljkaVersionARequest request)
    {
        var processId = Guid.NewGuid();

        var message = new SolveAttemptMessage(processId, request);

        if (!_channel.Writer.TryWrite(message))
        {
            return null;
        }

        return processId;
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var message in _channel.Reader.ReadAllAsync())
        {
            try
            {
                await ProcessSolveAttemptAsync(message);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Some error while processing solve attempt: {e.Message}");
            }
        }
    }

    private async Task ProcessSolveAttemptAsync(SolveAttemptMessage message)
    {
        var request = message.Request;
        var termsById = request.Terms.ToDictionary(x => x.Id);

        var orderedTemplates = request.Templates
            .Select(template =>
            {
                var krizaljka = TheKrizaljka.Create(template);
                var score = ScoreTemplate(krizaljka, request.ThemeTermIds, termsById);

                return new
                {
                    Template = template,
                    Score = score
                };
            })
            .Where(x => x.Score > int.MinValue)
            .OrderByDescending(x => x.Score)
            .Take(request.MaxTemplatesToTry)
            .ToList();

        var batchSize = Math.Max(1, request.MaxLayoutsPerTemplate);
        var solvedTarget = request.StopAfterSolvedTemplates.GetValueOrDefault(int.MaxValue);
        var solvedCount = 0;

        using var cts = new CancellationTokenSource();

        for (var i = 0; i < orderedTemplates.Count; i+= batchSize)
        {
            if (cts.IsCancellationRequested)
            {
                break;
            }

            var batch = orderedTemplates
                .Skip(i)
                .Take(batchSize)
                .ToList();

            var runningTasks = batch
                .Select(templateEntry => ProcessTemplateAsync(
                    templateEntry.Template,
                    request,
                    termsById,
                    cts.Token))
                .ToList();


            while (runningTasks.Count > 0)
            {
                var finishedTask = await Task.WhenAny(runningTasks);
                runningTasks.Remove(finishedTask);

                var processedTemplate = await finishedTask;

                await SaveProcessedTemplateAsync(
                    message.ProcessId,
                    processedTemplate);

                if (processedTemplate.IsSolved)
                {
                    solvedCount++;

                    if (solvedCount >= solvedTarget)
                    {
                        await cts.CancelAsync();
                    }
                }
            }
            if (solvedCount >= solvedTarget)
            {
                break;
            }
        }

    }

    private static async Task SaveProcessedTemplateAsync(Guid processId, ProcessedTemplate processedTemplate)
    {
        try
        {
            var currentSolveFolder = Path.Combine(ProcessedTemplatesStatesDir, processId.ToString());

            if (!Directory.Exists(currentSolveFolder))
            {
                Directory.CreateDirectory(currentSolveFolder);
            }

            var isSolvedText = processedTemplate.IsSolved ? "SOLVED" : "NOT_solved";
            var fileName = $"template_{processedTemplate.TemplateId}_{isSolvedText}.json";


            var processedTemplateJsonString = JsonSerializer.Serialize(processedTemplate, Options);
            await File.WriteAllTextAsync(Path.Combine(currentSolveFolder, fileName),
                processedTemplateJsonString);
        }
        catch (Exception e)
        {
            Console.WriteLine(
                $"SAVE FAILED: TemplateId: {processedTemplate.TemplateId}, isSolved: {processedTemplate.IsSolved}");
        }
    }

    private Task<ProcessedTemplate> ProcessTemplateAsync(
        KrizaljkaTemplate template,
        KrizaljkaVersionARequest request,
        Dictionary<long, Term> termsById,
        CancellationToken stopToken) => Task.Run(() => ProcessTemplate(template, request, termsById, stopToken));

    private ProcessedTemplate ProcessTemplate(
        KrizaljkaTemplate template,
        KrizaljkaVersionARequest request,
        IReadOnlyDictionary<long, Term> termsById,
        CancellationToken stopToken)
    {
        var analyzedKrizaljka = TheKrizaljka.Create(template);

        var layouts = BuildThemeLayouts(
            analyzedKrizaljka,
            request,
            termsById);

        var bestState = analyzedKrizaljka.State;
        var bestAssignedCount = analyzedKrizaljka.State.AssignedTermsBySlotId.Count;

        foreach (var layout in layouts)
        {
            if (stopToken.IsCancellationRequested)
            {
                break;
            }

            var freshKrizaljka = TheKrizaljka.Create(template);
            var creator = new KrizaljkaCreator(freshKrizaljka);

            var placedAllThemes = true;

            foreach (var placement in layout.Placements)
            {
                if (!creator.TryPlaceAssignedTermManually(
                        request.Terms,
                        placement.SlotId,
                        placement.TermId,
                        out _))
                {
                    placedAllThemes = false;
                    break;
                }
            }

            if (!placedAllThemes)
            {
                continue;
            }

            var solveResult = creator.TrySolve(
                request.Terms,
                request.MaxSolveMinutesPerTemplate,
                stopToken);

            if (solveResult.IsCreated)
            {
                return new ProcessedTemplate(
                    template.Id,
                    true,
                    solveResult.CurrentState);
            }

            var assignedCount = solveResult.BestState.AssignedTermsBySlotId.Count;

            if (assignedCount > bestAssignedCount)
            {
                bestAssignedCount = assignedCount;
                bestState = solveResult.BestState;
            }
        }

        return new ProcessedTemplate(
            template.Id,
            false,
            bestState);
    }

    private List<KrizaljkaThemeLayout> BuildThemeLayouts(
        TheKrizaljka krizaljka,
        KrizaljkaVersionARequest request,
        IReadOnlyDictionary<long, Term> termsById)
    {
        var themeTerms = request.ThemeTermIds
            .Select(id => termsById[id])
            .ToList();

        var candidateSlotsByTermId = new Dictionary<long, List<KrizaljkaSlot>>();

        foreach (var term in themeTerms)
        {
            var candidateSlots = krizaljka.Slots
                .Where(slot => slot.Length == term.Length)
                .OrderByDescending(slot => ScoreSlot(krizaljka, slot))
                .Take(request.MaxSlotsPerThemeTerm)
                .ToList();

            if (candidateSlots.Count == 0)
            {
                return [];
            }

            candidateSlotsByTermId[term.Id] = candidateSlots;
        }

        var orderedThemeTerms = themeTerms
            .OrderBy(term => candidateSlotsByTermId[term.Id].Count)
            .ThenByDescending(term => term.Length)
            .ThenBy(term => term.Id)
            .ToList();

        List<KrizaljkaThemeLayout> layouts = [];
        List<KrizaljkaThemePlacement> currentPlacements = [];
        HashSet<int> usedSlotIds = [];

        BuildThemeLayoutsRecursive(
            krizaljka,
            orderedThemeTerms,
            candidateSlotsByTermId,
            currentPlacements,
            usedSlotIds,
            layouts,
            request.MaxLayoutsPerTemplate);

        return layouts
            .OrderByDescending(x => x.Score)
            .ToList();
    }

    private void BuildThemeLayoutsRecursive(
        TheKrizaljka krizaljka,
        IReadOnlyList<Term> orderedThemeTerms,
        IReadOnlyDictionary<long, List<KrizaljkaSlot>> candidateSlotsByTermId,
        List<KrizaljkaThemePlacement> currentPlacements,
        HashSet<int> usedSlotIds,
        List<KrizaljkaThemeLayout> layouts,
        int maxLayoutsPerTemplate)
    {
        if (layouts.Count >= maxLayoutsPerTemplate)
        {
            return;
        }

        if (currentPlacements.Count == orderedThemeTerms.Count)
        {
            var score = ScoreLayout(krizaljka, currentPlacements, orderedThemeTerms);
            layouts.Add(new KrizaljkaThemeLayout(currentPlacements.ToList(), score));
            return;
        }

        var term = orderedThemeTerms[currentPlacements.Count];
        var candidateSlots = candidateSlotsByTermId[term.Id];

        foreach (var slot in candidateSlots)
        {
            if (usedSlotIds.Contains(slot.Id))
            {
                continue;
            }

            if (!IsCompatibleWithExistingPlacements(
                    krizaljka,
                    slot,
                    term,
                    currentPlacements,
                    orderedThemeTerms))
            {
                continue;
            }

            currentPlacements.Add(new KrizaljkaThemePlacement(slot.Id, term.Id));
            usedSlotIds.Add(slot.Id);

            BuildThemeLayoutsRecursive(
                krizaljka,
                orderedThemeTerms,
                candidateSlotsByTermId,
                currentPlacements,
                usedSlotIds,
                layouts,
                maxLayoutsPerTemplate);

            usedSlotIds.Remove(slot.Id);
            currentPlacements.RemoveAt(currentPlacements.Count - 1);

            if (layouts.Count >= maxLayoutsPerTemplate)
            {
                return;
            }
        }
    }

    private bool IsCompatibleWithExistingPlacements(
        TheKrizaljka krizaljka,
        KrizaljkaSlot candidateSlot,
        Term candidateTerm,
        IReadOnlyList<KrizaljkaThemePlacement> currentPlacements,
        IReadOnlyList<Term> allThemeTerms)
    {
        foreach (var existingPlacement in currentPlacements)
        {
            if (!krizaljka.SlotsById.TryGetValue(existingPlacement.SlotId, out var existingSlot))
            {
                return false;
            }

            var existingTerm = allThemeTerms.First(x => x.Id == existingPlacement.TermId);

            if (!AreSlotsCompatible(candidateSlot, candidateTerm, existingSlot, existingTerm))
            {
                return false;
            }
        }

        return true;
    }

    private bool AreSlotsCompatible(
        KrizaljkaSlot firstSlot,
        Term firstTerm,
        KrizaljkaSlot secondSlot,
        Term secondTerm)
    {
        for (var i = 0; i < firstSlot.CellKeys.Count; i++)
        {
            for (var j = 0; j < secondSlot.CellKeys.Count; j++)
            {
                if (firstSlot.CellKeys[i] != secondSlot.CellKeys[j])
                {
                    continue;
                }

                if (!string.Equals(
                        firstTerm.Letters[i],
                        secondTerm.Letters[j],
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private int ScoreLayout(
        TheKrizaljka krizaljka,
        IReadOnlyList<KrizaljkaThemePlacement> placements,
        IReadOnlyList<Term> allThemeTerms)
    {
        var total = 0;

        foreach (var placement in placements)
        {
            if (!krizaljka.SlotsById.TryGetValue(placement.SlotId, out var slot))
            {
                continue;
            }

            total += ScoreSlot(krizaljka, slot);
        }

        for (var i = 0; i < placements.Count; i++)
        {
            for (var j = i + 1; j < placements.Count; j++)
            {
                if (!krizaljka.SlotsById.TryGetValue(placements[i].SlotId, out var firstSlot))
                {
                    continue;
                }

                if (!krizaljka.SlotsById.TryGetValue(placements[j].SlotId, out var secondSlot))
                {
                    continue;
                }

                if (!DoSlotsIntersect(firstSlot, secondSlot))
                {
                    continue;
                }

                var firstTerm = allThemeTerms.First(x => x.Id == placements[i].TermId);
                var secondTerm = allThemeTerms.First(x => x.Id == placements[j].TermId);

                if (AreSlotsCompatible(firstSlot, firstTerm, secondSlot, secondTerm))
                {
                    total += 500;
                }
            }
        }

        return total;
    }

    private bool DoSlotsIntersect(KrizaljkaSlot firstSlot, KrizaljkaSlot secondSlot)
    {
        for (var i = 0; i < firstSlot.CellKeys.Count; i++)
        {
            for (var j = 0; j < secondSlot.CellKeys.Count; j++)
            {
                if (firstSlot.CellKeys[i] == secondSlot.CellKeys[j])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int ScoreTemplate(
    TheKrizaljka krizaljka,
    IReadOnlyList<long> themeTermIds,
    Dictionary<long, Term> termsById)
    {
        var total = 0;

        foreach (var termId in themeTermIds)
        {
            if (!termsById.TryGetValue(termId, out var term))
            {
                return int.MinValue;
            }

            var matchingSlots = krizaljka.Slots
                .Where(slot => slot.Length == term.Length)
                .ToList();

            if (matchingSlots.Count == 0)
            {
                return int.MinValue;
            }

            total += matchingSlots.Count * 100;

            total += matchingSlots
                .OrderByDescending(slot => ScoreSlot(krizaljka, slot))
                .Take(3)
                .Sum(slot => ScoreSlot(krizaljka, slot));
        }

        return total;
    }

    private static int ScoreSlot(TheKrizaljka krizaljka, KrizaljkaSlot slot) =>
    krizaljka.IntersectionsBySlotId.TryGetValue(slot.Id, out var intersections)
        ? intersections.Count
        : 0;
}
