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
        var candidatesBySlotId = GetCandidatesBySlotId(analysis.Slots, terms);
        return Solve(analysis.Slots, candidatesBySlotId, state);
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
        IReadOnlyDictionary<int, IReadOnlyList<Term>> candidatesBySlotId,
        KrizaljkaSolveState state)
    {
        var slotFittingCounts = GetSlotFittingCounts(slots, candidatesBySlotId, state);

        var unfittableSlots = slotFittingCounts.Where(x => x.FittingCount == 0).ToList();
        if (unfittableSlots.Count > 0)
        {
            //foreach (var unfittableSlot in unfittableSlots)
            //{
            //    Console.WriteLine($"Slot {unfittableSlot.SlotId} (len {unfittableSlot.SlotLength}) fitting: {unfittableSlot.FittingCount}");
            //}
            return false;
        }


        var nextSlot = GetNextSlot(slots, candidatesBySlotId, state);

        if (nextSlot is null)
        {
            return true;
        }

        if (!candidatesBySlotId.TryGetValue(nextSlot.Id, out var candidates))
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

            if (Solve(slots, candidatesBySlotId, state))
            {
                return true;
            }

            Undo(placement, state);
        }

        return false;
    }

    private static KrizaljkaSlot? GetNextSlot(
        IReadOnlyList<KrizaljkaSlot> slots,
        IReadOnlyDictionary<int, IReadOnlyList<Term>> candidatesBySlotId,
        KrizaljkaSolveState state)
    {
        KrizaljkaSlot? bestSlot = null;
        var bestCount = int.MaxValue;

        foreach (var slot in slots)
        {
            if (state.IsAssigned(slot.Id))
            {
                continue;
            }

            if (!candidatesBySlotId.TryGetValue(slot.Id, out var candidates))
            {
             //   return slot;
             continue;
            }

            var fittingCount = 0;

            foreach (var term in candidates)
            {
                if (Fits(slot, term, state))
                {
                    fittingCount++;
                }
            }

            if (fittingCount < bestCount)
            {
                bestCount = fittingCount;
                bestSlot = slot;

                //if (bestCount == 0)
                //{
                //    return bestSlot;
                //}
            }
        }

        return bestSlot;
    }

    private static IReadOnlyList<SlotFittingCount> GetSlotFittingCounts(
        IReadOnlyList<KrizaljkaSlot> slots,
        IReadOnlyDictionary<int, IReadOnlyList<Term>> candidatesBySlotId,
        KrizaljkaSolveState state)
    {
        List<SlotFittingCount> result = [];

        foreach (var slot in slots)
        {
            if (state.IsAssigned(slot.Id))
            {
                continue;
            }

            if (!candidatesBySlotId.TryGetValue(slot.Id, out var candidates))
            {
                result.Add(new SlotFittingCount(slot.Id, slot.Length, 0));
                continue;
            }

            var fittingCount = 0;

            foreach (var term in candidates)
            {
                if (Fits(slot, term, state))
                {
                    fittingCount++;
                }
            }

            result.Add(new SlotFittingCount(slot.Id, slot.Length, fittingCount));
        }

        return result.AsReadOnly();
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<Term>> GetCandidatesBySlotId(
        IReadOnlyList<KrizaljkaSlot> slots,
        IReadOnlyList<Term> terms)
    {
        Dictionary<int, IReadOnlyList<Term>> candidatesBySlotId = [];

        foreach (var slot in slots)
        {
            var candidates = terms
                .Where(x => x.Length == slot.Length)
                .ToList()
                .AsReadOnly();

            candidatesBySlotId.Add(slot.Id, candidates);
        }

        return candidatesBySlotId;
    }

    private static bool Fits(
        KrizaljkaSlot slot,
        Term term,
        KrizaljkaSolveState state)
    {
       // Console.WriteLine($"FITS: slotId: {slot.Id}, term: {term.RawValue}");
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
