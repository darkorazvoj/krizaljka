using Krizaljka.Domain.Caches;
using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Solver;

public sealed class KrizaljkaCreator
{
    private readonly CreatorCache _cache = new();

    public KrizaljkaCreateResult TrySolve(
        KrizaljkaTemplateAnalysis analysis,
        IReadOnlyList<Term> terms,
        KrizaljkaSolveState state)
    {
        var newState = state.DeepClone();
        EnsureTermsCaches(terms);

        var neighborSlotsIdsBySlotId = GetNeighborSlotIdBySlotId(analysis.Intersections);
        
        var slotsById = analysis.Slots.ToDictionary(x => x.Id);
        var solved = Solve(analysis.Slots, slotsById, neighborSlotsIdsBySlotId, newState);

        return new KrizaljkaCreateResult(solved, newState);
    }

    private void EnsureTermsCaches(IReadOnlyList<Term> terms)
    {
        if (GlobalCaches.TermsByLength.Count == 0)
        {
            GlobalCaches.TermsByLength = terms
                .GroupBy(x => x.Length)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<Term>)x.OrderBy(t => t.DenseValue, StringComparer.OrdinalIgnoreCase)
                        .ToList());
        }

        if (_cache.TermsByLengthPositionLetter.Count == 0)
        {
            var result = new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>>();

            foreach (var lengthGroup in terms.GroupBy(x => x.Length))
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

    public static bool TryPlaceAssignedTerm(
        KrizaljkaTemplateAnalysis analysis,
        IReadOnlyList<Term> terms,
        int slotId,
        long termId,
        KrizaljkaSolveState state, 
        out string? error)
    {
        error = null;

        if (state.IsAssigned(slotId))
        {
            error = "SlotAssigned";
            return false;
        }

        var slot = analysis.Slots.FirstOrDefault(x => x.Id == slotId);
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

        if (!Fits(slot, term, state))
        {
            error = "TermDoesNotFit";
            return false;
        }

        Place(slot, term, state);
        return true;
    }

    private  bool Solve(
        IReadOnlyList<KrizaljkaSlot> slots,
        IReadOnlyDictionary<int, KrizaljkaSlot> slotsById,
        IReadOnlyDictionary<int, IReadOnlyList<int>> neighborSlotsIdsBySlotId,
        KrizaljkaSolveState state)
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

            if (!PassesForwardCheck(nextSlot, slotsById, neighborSlotsIdsBySlotId, state))
            {
                Undo(placement, state);
                continue;
            }

            if (Solve(slots, slotsById, neighborSlotsIdsBySlotId, state))
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

    private static PlacementResult Place(
        KrizaljkaSlot slot,
        Term term,
        KrizaljkaSolveState state)
    {
       
        List<(int Row, int Cell)> newCells = [];

        state.AssignedTermsBySlotId.Add(
            slot.Id,
            new AssignedTerm(slot.Id, term.Id, term.Letters));

        state.UsedTermsIds.Add(term.Id);

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
      //  Console.WriteLine($"Place slotId: {slot.Id} {term.RawValue}");
        return new PlacementResult(slot.Id, term.Id, newCells.AsReadOnly());
    }

    private static void Undo(
        PlacementResult placement,
        KrizaljkaSolveState state)
    {
      //  Console.WriteLine($"UNDO slotId: {placement.SlotId}");
        state.AssignedTermsBySlotId.Remove(placement.SlotId);
        state.UsedTermsIds.Remove(placement.TermId);

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

    private static IReadOnlyDictionary<int, IReadOnlyList<int>> GetNeighborSlotIdBySlotId(
        IReadOnlyList<KrizaljkaIntersection> intersections)
    {
        Dictionary<int, HashSet<int>> map = [];

        foreach (var intersection in intersections)
        {
            if (!map.TryGetValue(intersection.FirstSlotId, out var first))
            {
                first = [];
                map.Add(intersection.FirstSlotId, first);
            }

            first.Add(intersection.SecondSlotId);

            if (!map.TryGetValue(intersection.SecondSlotId, out var second))
            {
                second = [];
                map.Add(intersection.SecondSlotId, second);
            }

            second.Add(intersection.FirstSlotId);
        }

        return map.ToDictionary(x => x.Key, x => (IReadOnlyList<int>)x.Value.ToList().AsReadOnly());
    }

    private  bool PassesForwardCheck(
        KrizaljkaSlot placedSlot,
        IReadOnlyDictionary<int, KrizaljkaSlot> slotsById,
        IReadOnlyDictionary<int, IReadOnlyList<int>> neighborSlotIdsBySlotId,
        KrizaljkaSolveState state)
    {
        if (!neighborSlotIdsBySlotId.TryGetValue(placedSlot.Id, out var neighborSlotsIds))
        {
            return true;
        }

        foreach (var neighborSlotId in neighborSlotsIds)
        {
            if (state.IsAssigned(neighborSlotId))
            {
                continue;
            }

            if (!slotsById.TryGetValue(neighborSlotId, out var neighborSlot))
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
