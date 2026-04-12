using Krizaljka.Domain.Caches;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Creator;

public sealed class KrizaljkaCreator(TheKrizaljka theKrizaljka)
{
    private CreatorCache _cache = new();
    private int _wordsPlacedDuringIterations;
    private IReadOnlyList<Term> _normalizedTerms = [];

    public KrizaljkaCreateResult TrySolve(IReadOnlyList<Term> terms)
    {
        _wordsPlacedDuringIterations = 0;
        _cache = new CreatorCache();

        _normalizedTerms = GetNormalizedTerms(terms);
        EnsureTermsCaches(_normalizedTerms);

        var solved = Solve(theKrizaljka.Slots);
        return new KrizaljkaCreateResult(solved, theKrizaljka.State, _wordsPlacedDuringIterations);
    }

    public bool TryPlaceAssignedTermManually(
        IReadOnlyList<Term> terms,
        int slotId,
        long termId,
        out string? error)
    {
        if (_normalizedTerms.Count == 0)
        {
            _normalizedTerms = GetNormalizedTerms(terms);
            EnsureTermsCaches(_normalizedTerms);
        }

        return TryPlaceAssignedTerm(
            _normalizedTerms,
            slotId,
            termId,
            out error);
    }

    private static IReadOnlyList<Term> GetNormalizedTerms(IReadOnlyList<Term> terms) =>
        terms
            .Select(t => t with
            {
                Letters = t.Letters.Select(x => x.NormalizeLetters()).ToArray()
            })
            .ToList()
            .AsReadOnly();

    private void EnsureTermsCaches(IReadOnlyList<Term> normalizedTerms)
    {
        if (GlobalCaches.TermsByLength.Count == 0)
        {
            GlobalCaches.TermsByLength = normalizedTerms
                .GroupBy(x => x.Length)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<Term>)x.OrderBy(t => t.DenseValue, StringComparer.OrdinalIgnoreCase)
                        .ToList());
        }

        if (_cache.TermsByLengthPositionLetter.Count == 0)
        {
            var result =
                new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>>();

            foreach (var lengthGroup in normalizedTerms.GroupBy(x => x.Length))
            {
                var positionMap = new Dictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>();

                for (var i = 0; i < lengthGroup.Key; i++)
                {
                    var letterMap = lengthGroup
                        .GroupBy(term => term.Letters[i])
                        .ToDictionary(
                            x => x.Key,
                            x => (IReadOnlyList<Term>)x
                                .OrderBy(t => t.DenseValue, StringComparer.OrdinalIgnoreCase)
                                .ToList());

                    positionMap.Add(i, letterMap);
                }

                result.Add(lengthGroup.Key, positionMap);
            }

            _cache.TermsByLengthPositionLetter = result;
        }
    }

    private bool TryPlaceAssignedTerm(
        IReadOnlyList<Term> terms,
        int slotId,
        long termId,
        out string? error)
    {
        error = null;

        if (theKrizaljka.State.IsAssigned(slotId))
        {
            error = "SlotAssigned";
            return false;
        }

        var slot = theKrizaljka.Slots.FirstOrDefault(x => x.Id == slotId);
        if (slot is null)
        {
            error = "SlotNotFound";
            return false;
        }

        var term = terms.FirstOrDefault(x => x.Id == termId);
        if (term is null)
        {
            error = "TermNotFound";
            return false;
        }

        if (!Fits(slot, term))
        {
            error = "TermDoesNotFit";
            return false;
        }

        Place(slot, term);
        return true;
    }

    private bool Solve(IReadOnlyList<KrizaljkaSlot> slots)
    {
        if (!TryGetBestNextSlot(slots, out var nextSlot))
        {
            return false;
        }

        if (nextSlot is null)
        {
            return true;
        }

        foreach (var term in GetOrderedTerms(nextSlot))
        {
            if (theKrizaljka.State.UsedTermsIds.Contains(term.Id))
            {
                continue;
            }

            if (!Fits(nextSlot, term))
            {
                continue;
            }

            var placement = Place(nextSlot, term);

            if (!PassesForwardCheck(nextSlot))
            {
                Undo(placement);
                continue;
            }

            if (Solve(slots))
            {
                return true;
            }

            Undo(placement);
        }

        return false;
    }

    private bool TryGetBestNextSlot(
    IReadOnlyList<KrizaljkaSlot> slots,
    out KrizaljkaSlot? bestSlot)
{
    bestSlot = null;
    var bestCount = int.MaxValue;
    var bestUnassignedNeighborCount = -1;

    foreach (var slot in slots)
    {
        if (theKrizaljka.State.IsAssigned(slot.Id))
        {
            continue;
        }

        var fittingCount = GetFittingCount(slot);

        if (fittingCount == 0)
        {
            return false;
        }

        var unassignedNeighborCount = GetUnassignedNeighborCount(slot);

        if (fittingCount < bestCount ||
            (fittingCount == bestCount && unassignedNeighborCount > bestUnassignedNeighborCount))
        {
            bestCount = fittingCount;
            bestUnassignedNeighborCount = unassignedNeighborCount;
            bestSlot = slot;

            if (bestCount == 1)
            {
                return true;
            }
        }
    }

    return true;
}

    private int GetUnassignedNeighborCount(KrizaljkaSlot slot)
    {
        if (!theKrizaljka.NeighborSlotsIdsBySlotId.TryGetValue(slot.Id, out var neighborSlotIds))
        {
            return 0;
        }

        var count = 0;

        foreach (var neighborSlotId in neighborSlotIds)
        {
            if (!theKrizaljka.State.IsAssigned(neighborSlotId))
            {
                count++;
            }
        }

        return count;
    }

    private bool Fits(KrizaljkaSlot slot, Term term)
    {
        if (theKrizaljka.State.IsAssigned(slot.Id))
        {
            return false;
        }

        if (theKrizaljka.State.UsedTermsIds.Contains(term.Id))
        {
            return false;
        }

        if (term.Length != slot.Length)
        {
            return false;
        }

        for (var i = 0; i < slot.Cells.Count; i++)
        {
            var termLetter = term.Letters[i];

            if (theKrizaljka.State.LettersByCell.TryGetValue(slot.CellKeys[i], out var existingLetter) &&
                !string.Equals(existingLetter, termLetter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private PlacementResult Place(KrizaljkaSlot slot, Term term)
    {
        List<(int Row, int Col)> newCells = [];
        List<(int SlotId, long TermId)> assignedSlots = [];

        AssignSlot(slot, term, newCells, assignedSlots);

        Queue<int> queue = new();
        queue.Enqueue(slot.Id);

        HashSet<int> queuedOrProcessed = [slot.Id];

        while (queue.Count > 0)
        {
            var currentSlotId = queue.Dequeue();

            if (!theKrizaljka.IntersectionsBySlotId.TryGetValue(currentSlotId, out var intersections))
            {
                continue;
            }

            foreach (var intersection in intersections)
            {
                var neighborSlotId = intersection.FirstSlotId == currentSlotId
                    ? intersection.SecondSlotId
                    : intersection.FirstSlotId;

                if (!queuedOrProcessed.Add(neighborSlotId))
                {
                    continue;
                }

                if (theKrizaljka.State.IsAssigned(neighborSlotId))
                {
                    continue;
                }

                if (!theKrizaljka.SlotsById.TryGetValue(neighborSlotId, out var neighborSlot))
                {
                    continue;
                }

                if (!IsFullyFilled(neighborSlot))
                {
                    continue;
                }

                var matchingTerms = GetIndexedMatchingTerms(neighborSlot)
                    .Where(x => !theKrizaljka.State.UsedTermsIds.Contains(x.Id))
                    .ToList();

                if (matchingTerms.Count != 1)
                {
                    continue;
                }

                AssignSlot(neighborSlot, matchingTerms[0], newCells, assignedSlots);
                queue.Enqueue(neighborSlotId);
            }
        }

        return new PlacementResult(assignedSlots.AsReadOnly(), newCells.AsReadOnly());
    }

    private bool IsFullyFilled(KrizaljkaSlot slot)
    {
        for (var i = 0; i < slot.Cells.Count; i++)
        {
            if (!theKrizaljka.State.LettersByCell.ContainsKey(slot.CellKeys[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void AssignSlot(
        KrizaljkaSlot slot,
        Term term,
        List<(int Row, int Col)> newCells,
        List<(int SlotId, long TermId)> assignedSlots)
    {
        _wordsPlacedDuringIterations++;
        theKrizaljka.State.AssignedTermsBySlotId.Add(
            slot.Id,
            new AssignedTerm(slot.Id, term.Id, term.Letters));

        theKrizaljka.State.UsedTermsIds.Add(term.Id);
        assignedSlots.Add((slot.Id, term.Id));

        for (var i = 0; i < slot.Cells.Count; i++)
        {
            var slotCell = slot.Cells[i];
            var key = (slotCell.Row, slotCell.Col);

            if (!theKrizaljka.State.LettersByCell.ContainsKey(key))
            {
                theKrizaljka.State.LettersByCell.Add(key, term.Letters[i]);
                newCells.Add(key);
            }
        }
    }

    private void Undo(PlacementResult placement)
    {
        foreach (var (slotId, termId) in placement.AssignedSlots)
        {
            theKrizaljka.State.AssignedTermsBySlotId.Remove(slotId);
            theKrizaljka.State.UsedTermsIds.Remove(termId);
        }

        foreach (var cell in placement.NewCells)
        {
            theKrizaljka.State.LettersByCell.Remove(cell);
        }
    }

    private IReadOnlyList<Term> GetIndexedMatchingTerms(KrizaljkaSlot slot)
    {
        var pattern = GetSlotPattern(slot);
        var cacheKey = (slot.Id, pattern);

        if (_cache.MatchingTermsCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var result = GetIndexedMatchingTermsCore(slot);
        _cache.MatchingTermsCache[cacheKey] = result;

        return result;
    }

    private string GetSlotPattern(KrizaljkaSlot slot)
    {
        var state = theKrizaljka.State;
        //var chars = new char[slot.Cells.Count];
        var parts = new string[slot.Cells.Count];


        for (var i = 0; i < slot.Cells.Count; i++)
        {

            if (state.LettersByCell.TryGetValue(slot.CellKeys[i], out var letter) &&
                !string.IsNullOrWhiteSpace(letter))
            {
                parts[i] = letter.NormalizeLetters();
            }
            else
            {
                parts[i] = "_";
            }
        }

        return string.Join('|', parts);
    }

    private IReadOnlyList<Term> GetIndexedMatchingTermsCore(KrizaljkaSlot slot)
    {
        if (!GlobalCaches.TermsByLength.TryGetValue(slot.Length, out var allTerms))
        {
            return [];
        }

        if (!_cache.TermsByLengthPositionLetter.TryGetValue(slot.Length, out var byPosition))
        {
            return allTerms;
        }

        List<IReadOnlyList<Term>> constrainedLists = [];

        for (var i = 0; i < slot.Cells.Count; i++)
        {
            if (!theKrizaljka.State.LettersByCell.TryGetValue(slot.CellKeys[i], out var existingLetter))
            {
                continue;
            }

            if (!byPosition.TryGetValue(i, out var byLetter))
            {
                return [];
            }

            var normalizedExistingLetter = existingLetter.NormalizeLetters();

            if (!byLetter.TryGetValue(normalizedExistingLetter, out var matchingTerms))
            {
                return [];
            }
            
            constrainedLists.Add(matchingTerms);
        }

        if (constrainedLists.Count == 0)
        {
            return allTerms;
        }

        var smallest = constrainedLists
            .OrderBy(x => x.Count)
            .First();

        List<Term> result = [];

        foreach (var term in smallest)
        {
            var matches = true;

            for (var i = 0; i < slot.Cells.Count; i++)
            {
                if (theKrizaljka.State.LettersByCell.TryGetValue(slot.CellKeys[i], out var existingLetter) &&
                    !string.Equals(term.Letters[i], existingLetter.NormalizeLetters(), StringComparison.Ordinal))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                result.Add(term);
            }
        }

        return result;
    }

    private bool PassesForwardCheck(KrizaljkaSlot placedSlot)
    {
        if (!theKrizaljka.IntersectionsBySlotId.TryGetValue(placedSlot.Id, out var intersections))
        {
            return true;
        }

        foreach (var intersection in intersections)
        {
            var neighborSlotId = intersection.FirstSlotId == placedSlot.Id
                ? intersection.SecondSlotId
                : intersection.FirstSlotId;

            if (theKrizaljka.State.IsAssigned(neighborSlotId))
            {
                continue;
            }

            if (!theKrizaljka.SlotsById.TryGetValue(neighborSlotId, out var neighborSlot))
            {
                return false;
            }

            var hasAnythingFitting = false;

            foreach (var term in GetIndexedMatchingTerms(neighborSlot))
            {
                if (theKrizaljka.State.UsedTermsIds.Contains(term.Id))
                {
                    continue;
                }

                hasAnythingFitting = true;
                break;
            }

            if (!hasAnythingFitting)
            {
                return false;
            }
        }

        return true;
    }

    private int GetFittingCount(KrizaljkaSlot slot)
    {
        var count = 0;
        foreach (var term in GetIndexedMatchingTerms(slot))
        {
            if (!theKrizaljka.State.UsedTermsIds.Contains(term.Id))
            {
                count++;
            }
        }

        return count;
    }

   private List<Term> GetOrderedTerms(KrizaljkaSlot slot)
    {
        List<Term> result = [];

        foreach (var term in GetIndexedMatchingTerms(slot))
        {
            if (theKrizaljka.State.UsedTermsIds.Contains(term.Id))
            {
                continue;
            }

            result.Add(term);
        }

        return result;
    }
}
