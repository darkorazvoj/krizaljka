using Krizaljka.Domain.Template;
using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Creator;

public sealed class KrizaljkaVersionASolver
{
    private record KrizaljkaVersionAWorkItem(KrizaljkaTemplate Template, KrizaljkaThemeLayout Layout);

    public async Task<KrizaljkaVersionAResult> TrySolveAsync(KrizaljkaVersionARequest request)
    {
        var termsById = request.Terms.ToDictionary(x => x.Id);

        var orderedTemplates = request.Templates
            .Select(template =>
            {
                var krizaljka = TheKrizaljka.Create(template, new KrizaljkaSolveState());
                var score = ScoreTemplate(krizaljka, request.ThemeTermIds, termsById);

                return new { Template = template, Score = score, Krizaljka = krizaljka };
            })
            .Where(x => x.Score > int.MinValue)
            .OrderByDescending(x => x.Score)
            .Take(request.MaxTemplatesToTry)
            .ToList();
        
        List<KrizaljkaVersionAWorkItem> workItems = [];
        foreach (var templateEntry in orderedTemplates)
        {
            var template = templateEntry.Template;
            var analyzedKrizaljka = templateEntry.Krizaljka;

            var layouts = BuildThemeLayouts(
                analyzedKrizaljka,
                request,
                termsById);

            foreach (var layout in layouts)
            {
                workItems.Add(new KrizaljkaVersionAWorkItem(template, layout));
            }
        }

        KrizaljkaVersionAResult? bestPartial = null;
        const int batchSize = 5;

        for (var i = 0; i < workItems.Count; i++)
        {
            var batch = workItems
                .Skip(i)
                .Take(batchSize)
                .ToList();

            using var cts = new CancellationTokenSource();

            var tasks = batch
                .Select(wi => Task.Run(() => SolveWorkItem(wi, request, cts.Token), CancellationToken.None))
                .ToList();

            var results = await Task.WhenAll(tasks);
            var solved = results.FirstOrDefault(x => x.Solved);
            if (solved is not null)
            {
                await cts.CancelAsync();
                return solved;
            }

            var bestInBatch = results
                .Where(x => x.CreateResult is not null)
                .OrderByDescending(x => x.CreateResult!.Stats.MaxAssignedSlotsReached)
                .FirstOrDefault();

            if (bestInBatch is not null &&
                (bestPartial is null ||
                 bestInBatch.CreateResult!.Stats.MaxAssignedSlotsReached >
                 bestPartial.CreateResult!.Stats.MaxAssignedSlotsReached))
            {
                bestPartial = bestInBatch;
            }
        }

        return bestPartial ?? new KrizaljkaVersionAResult(
            false,
            null,
            [],
            null);
    }

    private KrizaljkaVersionAResult SolveWorkItem(
        KrizaljkaVersionAWorkItem workItem,
        KrizaljkaVersionARequest request,
        CancellationToken stopToken)
    {
        var freshKrizaljka = TheKrizaljka.Create(workItem.Template, new KrizaljkaSolveState());
        var creator = new KrizaljkaCreator(freshKrizaljka);

        var placedAllThemes = true;
        List<KrizaljkaThemePlacement> placedThemes = [];

        foreach (var placement in workItem.Layout.Placements)
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

            placedThemes.Add(placement);
        }

        if (!placedAllThemes)
        {
            return new KrizaljkaVersionAResult(false, workItem.Template, placedThemes, null);
        }

        var solveResult = creator.TrySolve(
            request.Terms,
            request.MaxSolveMinutesPerLayout,
            stopToken);

        return new KrizaljkaVersionAResult(
            solveResult.IsCreated,
            workItem.Template,
            placedThemes,
            solveResult);
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

    private List<KrizaljkaThemeLayout> BuildThemeLayouts(
        TheKrizaljka krizaljka,
        KrizaljkaVersionARequest request,
        Dictionary<long, Term> termsById)
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
                .Take(request.MaxSlotPerThemeTerm)
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
        HashSet<int> usedSlotsId = [];

        BuildThemeLayoutsRecursive(
            krizaljka,
            orderedThemeTerms,
            candidateSlotsByTermId,
            currentPlacements,
            usedSlotsId,
            layouts,
            request.MaxLayoutsPerTemplate);

        return layouts
            .OrderByDescending(x => x.Score)
            .ToList();
    }

    private static void BuildThemeLayoutsRecursive(
        TheKrizaljka krizaljka,
        IReadOnlyList<Term> orderedThemeTerms,
        IReadOnlyDictionary<long, List<KrizaljkaSlot>> candidateSlotsByTermId,
        List<KrizaljkaThemePlacement> currentPlacement,
        HashSet<int> usedSlotIds,
        List<KrizaljkaThemeLayout> layouts,
        int maxLayoutsPerTemplate)
    {
        if (layouts.Count >= maxLayoutsPerTemplate)
        {
            return;
        }

        if (currentPlacement.Count == orderedThemeTerms.Count)
        {
            var score = ScoreLayout(krizaljka, currentPlacement, orderedThemeTerms);
            layouts.Add(new KrizaljkaThemeLayout(currentPlacement.ToList(), score));
            return;
        }

        var term = orderedThemeTerms[currentPlacement.Count];
        var candidateSlots = candidateSlotsByTermId[term.Id];

        foreach (var slot in candidateSlots)
        {
            if (usedSlotIds.Contains(slot.Id))
            {
                continue;
            }

            if (!IsCompatibleWithExistingPlacement(
                    krizaljka,
                    slot,
                    term,
                    currentPlacement,
                    orderedThemeTerms))
            {
                continue;
            }

            currentPlacement.Add(new KrizaljkaThemePlacement(slot.Id, term.Id));
            usedSlotIds.Add(slot.Id);

            BuildThemeLayoutsRecursive(
                krizaljka,
                orderedThemeTerms,
                candidateSlotsByTermId,
                currentPlacement,
                usedSlotIds,
                layouts,
                maxLayoutsPerTemplate);

            usedSlotIds.Remove(slot.Id);
            currentPlacement.RemoveAt(currentPlacement.Count - 1);

            if (layouts.Count >= maxLayoutsPerTemplate)
            {
                return;
            }
        }
    }

    private static bool IsCompatibleWithExistingPlacement(
        TheKrizaljka krizaljka,
        KrizaljkaSlot candidateSlot,
        Term candiTerm,
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

            if (!AreSlotsCompatible(candidateSlot, candiTerm, existingSlot, existingTerm))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreSlotsCompatible(
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

    private static int ScoreLayout(
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
            for (var j = 0; j < placements.Count; j++)
            {
                if(!krizaljka.SlotsById.TryGetValue(placements[i].SlotId, out var firstSlot))
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

    private static bool DoSlotsIntersect(KrizaljkaSlot firstSlot, KrizaljkaSlot secondSlot)
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
}
