using Krizaljka.Domain.Template;
using Krizaljka.Domain.TemplateAnalysis;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Krizaljka.Domain.Creator;

namespace Krizaljka.Domain;

public sealed class TheKrizaljka
{
    public IReadOnlyList<KrizaljkaSlot> Slots { get; private set; } = [];

    public IReadOnlyDictionary<int, KrizaljkaSlot> SlotsById { get; private set; } =
        ImmutableDictionary<int, KrizaljkaSlot>.Empty;
    public IReadOnlyList<KrizaljkaIntersection> Intersections { get; private set; } = [];
    public IReadOnlyDictionary<int, IReadOnlyList<KrizaljkaIntersection>> IntersectionsBySlotId { get; private set; } = 
        ImmutableDictionary<int, IReadOnlyList<KrizaljkaIntersection>>.Empty;

    public IReadOnlyDictionary<int, IReadOnlyList<int>> NeighborSlotsIdsBySlotId { get; private set; } =
        ImmutableDictionary<int, IReadOnlyList<int>>.Empty;
    public ReadOnlyDictionary<(int, int), List<SlotUsage>> CellSlots { get; private set; } =
        new(new Dictionary<(int, int), List<SlotUsage>>());
    
    public KrizaljkaTemplate Template { get; }
    public KrizaljkaSolveState State { get; private set; }

    public IReadOnlyList<AssignedTerm> AssignedTerms => State.AssignedTermsBySlotId.Values.ToList().AsReadOnly();
    

    private TheKrizaljka(
        KrizaljkaTemplate template,
        KrizaljkaSolveState state)
    {
        Template = template;
        State = state;
    }

    private void Init()
    {
        var analysis = new KrizaljkaAnalyzer().GeTemplateAnalysis(Template);
        Slots = analysis.Slots;
        SlotsById = Slots.ToDictionary(x => x.Id);
        Intersections = analysis.Intersections;
        IntersectionsBySlotId = analysis.IntersectionsBySlotId;
        NeighborSlotsIdsBySlotId = analysis.NeighborSlotsIdsBySlotId;
        CellSlots = analysis.CellSlots.AsReadOnly();
    }

    public static TheKrizaljka Create(
        KrizaljkaTemplate template,
        KrizaljkaSolveState? state = null)
    {

        var krizaljka = new TheKrizaljka(template, state ?? new KrizaljkaSolveState());
        krizaljka.Init();
        return krizaljka;
    }

    public bool ClearSlot(int slotId)
    {
        if (!State.AssignedTermsBySlotId.Remove(slotId, out var assignedTerm))
        {
            return false;
        }

        State.UsedTermsIds.Remove(assignedTerm.TermId);

        var slot = Slots.FirstOrDefault(x => x.Id == slotId);
        if (slot is null)
        {
            return false;
        }

        foreach (var cell in slot.Cells)
        {
            var key = (cell.Row, cell.Col);
            var shouldKeepLetter = false;

            if (CellSlots.TryGetValue(key, out var cellSlotIds))
            {
                foreach (var usage in cellSlotIds)
                {
                    if (usage.SlotId == slotId)
                    {
                        continue;
                    }

                    if (State.AssignedTermsBySlotId.ContainsKey(usage.SlotId))
                    {
                        shouldKeepLetter = true;
                        break;
                    }
                }
            }

            if (!shouldKeepLetter)
            {
                State.LettersByCell.Remove(key);
            }
        }

        return true;
    }

    public void ReplaceState(KrizaljkaSolveState state) => State = state;
}
