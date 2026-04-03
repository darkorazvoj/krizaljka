using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Solver;

public sealed class KrizaljkaSolver
{
    public static bool TrySolve(
        KrizaljkaTemplateAnalysis analysis,
        IReadOnlyList<Term> terms,
        KrizaljkaSolvedState state)
    {
        var candidatesBySlotId = GetCandidatesBySlotId(analysis.Slots, terms);
        return Solve(analysis.Slots, candidatesBySlotId, state);
    }

    public static bool TryPlaceAssignedTerm(
        KrizaljkaTemplateAnalysis analysis,
        IReadOnlyList<Term> terms,
        int slotId,
        long termId,
        KrizaljkaSolvedState state, 
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
        KrizaljkaSolvedState state)
    {
        KrizaljkaSlot? nextSlot = null;

        foreach (var slot in slots)
        {
            if (!state.IsAssigned(slot.Id))
            {
                nextSlot = slot;
                break;
            }
        }

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
        KrizaljkaSolvedState state)
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
        KrizaljkaSolvedState state)
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
        Console.WriteLine($"Place slotId: {slot.Id} {term.RawValue}");
        return new PlacementResult(slot.Id, term.Id, newCells.AsReadOnly());
    }

    private static void Undo(
        PlacementResult placement,
        KrizaljkaSolvedState state)
    {
        Console.WriteLine($"UNDO slotId: {placement.SlotId}");
        state.AssignedTermsBySlotId.Remove(placement.SlotId);
        state.UsedTermsIds.Remove(placement.TermId);

        foreach (var cell in placement.NewCells)
        {
            state.LettersByCell.Remove(cell);
        }
    }

}
