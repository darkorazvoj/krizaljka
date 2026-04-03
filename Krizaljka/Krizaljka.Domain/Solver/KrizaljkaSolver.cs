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
        if (CachedTerms.TermsByLength.Count == 0)
        {
            CachedTerms.TermsByLength = terms
                .GroupBy(x => x.Length)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<Term>)x.OrderBy(t => t.DenseValue, StringComparer.OrdinalIgnoreCase)
                        .ToList());
        }

        
        var slotsById = analysis.Slots.ToDictionary(x => x.Id);
        return Solve(analysis.Slots, slotsById, state);
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

        foreach (var term in candidates)
        {
            if (!Fits(nextSlot, term, state))
            {
                continue;
            }

            var placement = Place(nextSlot, term, state);

            if (Solve(slots, slotsById, state))
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

            foreach (var term in candidates)
            {
                if (Fits(slot, term, state))
                {
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

}
