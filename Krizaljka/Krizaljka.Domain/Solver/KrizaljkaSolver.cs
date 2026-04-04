using Krizaljka.Domain.Caches;
using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Solver;

public sealed class KrizaljkaSolver
{
    public static bool TrySolve(
        KrizaljkaTemplateAnalysis analysis,
        IReadOnlyList<Term> terms,
        KrizaljkaSolveState state)
    {
        EnsureTermsByLengthCache(terms);

        var neighborSlotsIdsBySlotId = GetNeighborSlotIdBySlotId(analysis.Intersections);
        
        var slotsById = analysis.Slots.ToDictionary(x => x.Id);
        return Solve(analysis.Slots, slotsById, neighborSlotsIdsBySlotId, state);
    }

    private static void EnsureTermsByLengthCache(IReadOnlyList<Term> terms)
    {
        if (CachedTerms.TermsByLength.Count == 0)
        {
            CachedTerms.TermsByLength = terms
                .GroupBy(x => x.Length)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<Term>)x.OrderBy(t => t.DenseValue, StringComparer.OrdinalIgnoreCase)
                        .ToList());
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

    private static bool Solve(
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

        if (!CachedTerms.TermsByLength.TryGetValue(nextSlot.Length, out var candidates))
        {
            return false;
        }

        foreach (var term in GetMatchingTerms(nextSlot, candidates, state))
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

    private static bool TryGetBestNextSlot(
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

            if (!CachedTerms.TermsByLength.TryGetValue(slot.Length, out var candidates))
            {
                return false;
            }

            var fittingCount = 0;

            foreach (var term in GetMatchingTerms(slot, candidates, state))
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

    private static IEnumerable<Term> GetMatchingTerms(
        KrizaljkaSlot slot,
        IReadOnlyList<Term> terms,
        KrizaljkaSolveState state)
    {
        foreach (var term in terms)
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
                yield return term;
            }
        }

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

    private static bool PassesForwardCheck(
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

            if (!CachedTerms.TermsByLength.TryGetValue(neighborSlot.Length, out var candidates))
            {
                return false;
            }

            var hasAnythingFitting = false;

            foreach (var term in GetMatchingTerms(neighborSlot, candidates, state))
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
