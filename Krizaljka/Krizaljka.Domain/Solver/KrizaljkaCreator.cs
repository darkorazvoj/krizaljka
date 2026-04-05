using Krizaljka.Domain.Caches;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Solver;

public sealed class KrizaljkaCreator(TheKrizaljka theKrizaljka)
{   
    private readonly CreatorCache _cache = new();
    private int _wordsPlacedDuringIterations;

    public KrizaljkaCreateResult TrySolve(IReadOnlyList<Term> terms)
    {
        _wordsPlacedDuringIterations = 0;
        EnsureTermsCaches(terms);

        var solved = Solve(theKrizaljka.Slots, theKrizaljka.State);

        return new KrizaljkaCreateResult(solved, theKrizaljka.State, _wordsPlacedDuringIterations);
    }

    public bool TryPlaceAssignedTermManually(
        IReadOnlyList<Term> terms,
        int slotId,
        long termId,
        out string? error)
    {   
        return TryPlaceAssignedTerm(
            terms,
            slotId,
            termId,
            out error);
    }

    private IReadOnlyList<Term> GetNormalizedTerms(IReadOnlyList<Term> terms) =>
        terms
            .Select(t => t with
            {
                Letters = t.Letters.Select(x => x.NormalizeLetters()).ToArray()
            })
            .ToList()
            .AsReadOnly();

    private void EnsureTermsCaches(IReadOnlyList<Term> terms)
    {
        IReadOnlyList<Term>? normalizedTerms = null;

        if (GlobalCaches.TermsByLength.Count == 0)
        {
            normalizedTerms ??= GetNormalizedTerms(terms);

            GlobalCaches.TermsByLength = normalizedTerms
                .GroupBy(x => x.Length)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<Term>)x.OrderBy(t => t.DenseValue, StringComparer.OrdinalIgnoreCase)
                        .ToList());
        }

        if (_cache.TermsByLengthPositionLetter.Count == 0)
        {
            normalizedTerms ??= GetNormalizedTerms(terms);

            var result = new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>>();

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

        if (!Fits(slot, term, theKrizaljka.State))
        {
            error = "TermDoesNotFit";
            return false;
        }

        Place(slot, term, theKrizaljka.State);
        return true;
    }

    private bool Solve(IReadOnlyList<KrizaljkaSlot> slots, KrizaljkaSolveState state)
    {
        if (!TryGetBestNextSlot(slots, state, out var nextSlot))
        {
            return false;
        }

        if (nextSlot is null)
        {
            return true;
        }

        foreach (var term in GetIndexedMatchingTerms(nextSlot, state))
        {
            if (state.UsedTermsIds.Contains(term.Id))
            {
                continue;
            }

            if (!Fits(nextSlot, term, state))
            {
                continue;
            }
            
            var placement = Place(nextSlot, term, state);

            if (!PassesForwardCheck(nextSlot, state))
            {
                Undo(placement, state);
                continue;
            }

            if (Solve(slots, state))
            {
                return true;
            }

            Undo(placement, state);

        }

        return false;
    }

    private  bool TryGetBestNextSlot(
        IReadOnlyList<KrizaljkaSlot> slots, 
        KrizaljkaSolveState state, 
        out KrizaljkaSlot? bestSlot)
    {
        bestSlot = null;
        var bestCount = int.MaxValue;
        var hasUnassignedSlots = false;

        foreach (var slot in slots)
        {
            if (state.IsAssigned(slot.Id))
            {
                continue;
            }

            hasUnassignedSlots = true;
            var fittingCount = 0;

            foreach (var term in GetIndexedMatchingTerms(slot, state))
            {
                if (state.UsedTermsIds.Contains(term.Id))
                {
                    continue;
                }

                fittingCount++;
            }

            if (fittingCount == 0)
            {
                return false;
            }

            if (fittingCount < bestCount)
            {
                bestCount = fittingCount;
                bestSlot = slot;

                if (bestCount == 1)
                {
                    return true;
                }
            }
        }

        if (!hasUnassignedSlots)
        {
            bestSlot = null;
            return true;
        }

        return bestSlot is not null;
    }
    
    private static bool Fits(
        KrizaljkaSlot slot,
        Term term,
        KrizaljkaSolveState state)
    {
        if (state.IsAssigned(slot.Id))
        {
            return false;
        }

        if (state.UsedTermsIds.Contains(term.Id))
        {
            return false;
        }

        if (term.Length != slot.Length)
        {
            return false;
        }

        for (var i = 0; i < slot.Cells.Count; i++)
        {
            var slotCell = slot.Cells[i];
            var termLetter = term.Letters[i];

            var key = (slotCell.Row, slotCell.Col);

            if (state.LettersByCell.TryGetValue(key, out var existingLetter) &&
                !existingLetter.Equals(termLetter, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private PlacementResult Place(
        KrizaljkaSlot slot,
        Term term,
        KrizaljkaSolveState state)
    {
        List<(int Row, int Col)> newCells = [];
        List<(int SlotId, long TermId)> assignedSlots = [];

        AssignSlot(slot, term, state, newCells, assignedSlots);

        Queue<int> queue = new();
        queue.Enqueue(slot.Id);

        HashSet<int> queuedOrProcesses = [slot.Id];

        while (queue.Count > 0)
        {
            var currentSlotId = queue.Dequeue();

            if (!theKrizaljka.NeighborSlotsIdsBySlotId.TryGetValue(currentSlotId, out var neighborSlotsIds))
            {
                continue;
            }

            foreach (var neighborSlotId in neighborSlotsIds)
            {
                if (!queuedOrProcesses.Add(neighborSlotId))
                {
                    continue;
                }

                if (state.IsAssigned(neighborSlotId))
                {
                    continue;
                }

                if (!theKrizaljka.SlotsById.TryGetValue(neighborSlotId, out var neighborSlot))
                {
                    continue;
                }

                if (!IsFullyFilled(neighborSlot, state))
                {
                    continue;
                }

                var matchingTerms = GetIndexedMatchingTerms(neighborSlot, state)
                    .Where(x => !state.UsedTermsIds.Contains(x.Id))
                    .ToList();

                if (matchingTerms.Count != 1)
                {
                    continue;
                }

                AssignSlot(neighborSlot, matchingTerms[0], state, newCells, assignedSlots);
                queue.Enqueue(neighborSlotId);
            }
        }

        return new PlacementResult(assignedSlots.AsReadOnly(), newCells.AsReadOnly());
    }

    private static bool IsFullyFilled(
        KrizaljkaSlot slot,
        KrizaljkaSolveState state)
    {
        for (var i = 0; i < slot.Cells.Count; i++)
        {
            var cell = slot.Cells[i];
            var key = (cell.Row, cell.Col);

            if (!state.LettersByCell.ContainsKey(key))
            {
                return false;
            }
        }

        return true;
    }

    private  void AssignSlot(
        KrizaljkaSlot slot,
        Term term,
        KrizaljkaSolveState state,
        List<(int Row, int Col)> newCells,
        List<(int SlotId, long TermId)> assignedSlots)
    {
        _wordsPlacedDuringIterations++;
        state.AssignedTermsBySlotId.Add(
            slot.Id,
            new AssignedTerm(slot.Id, term.Id, term.Letters));

        state.UsedTermsIds.Add(term.Id);
        assignedSlots.Add((slot.Id, term.Id));

        for (var i = 0; i < slot.Cells.Count; i++)
        {
            var slotCell = slot.Cells[i];
            var key = (slotCell.Row, slotCell.Col);

            if (!state.LettersByCell.ContainsKey(key))
            {
                state.LettersByCell.Add(key, term.Letters[i]);
                newCells.Add(key);
            }
        }
    }

    private static void Undo(
        PlacementResult placement,
        KrizaljkaSolveState state)
    {
        foreach (var (slotId, termId) in placement.AssignedSlots)
        {
            state.AssignedTermsBySlotId.Remove(slotId);
            state.UsedTermsIds.Remove(termId);
        }

        foreach (var cell in placement.NewCells)
        {
            state.LettersByCell.Remove(cell);
        }
    }

    private  IReadOnlyList<Term> GetIndexedMatchingTerms(
        KrizaljkaSlot slot,
        KrizaljkaSolveState state)
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
            var cell = slot.Cells[i];
            var key = (cell.Row, cell.Col);

            if (!state.LettersByCell.TryGetValue(key, out var existingLetter))
            {
                continue;
            }

            if (!byPosition.TryGetValue(i, out var byLetter))
            {
                return [];
            }

            if (!byLetter.TryGetValue(existingLetter, out var matchingTerms))
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
                var cell = slot.Cells[i];
                var key = (cell.Row, cell.Col);

                if (state.LettersByCell.TryGetValue(key, out var existingLetter) &&
                    term.Letters[i] != existingLetter)
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
    private  bool PassesForwardCheck(KrizaljkaSlot placedSlot, KrizaljkaSolveState state)
    {
        if (!theKrizaljka.NeighborSlotsIdsBySlotId.TryGetValue(placedSlot.Id, out var neighborSlotsIds))
        {
            return true;
        }

        foreach (var neighborSlotId in neighborSlotsIds)
        {
            if (state.IsAssigned(neighborSlotId))
            {
                continue;
            }

            if (!theKrizaljka.SlotsById.TryGetValue(neighborSlotId, out var neighborSlot))
            {
                return false;
            }

            var hasAnythingFitting = false;

            foreach (var term in GetIndexedMatchingTerms(neighborSlot, state))
            {
                if (state.UsedTermsIds.Contains(term.Id))
                {
                    continue;
                }

                if (!Fits(neighborSlot, term, state))
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

}
