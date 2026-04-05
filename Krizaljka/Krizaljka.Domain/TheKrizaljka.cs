using System.Collections.ObjectModel;
using Krizaljka.Domain.Solver;
using Krizaljka.Domain.Template;
using Krizaljka.Domain.TemplateAnalysis;

namespace Krizaljka.Domain;

public sealed class TheKrizaljka
{
    public IReadOnlyList<KrizaljkaSlot> Slots { get; private set; } = [];
    public IReadOnlyList<KrizaljkaIntersection> Intersections { get; private set; } = [];
    public ReadOnlyDictionary<(int, int), List<SlotUsage>> CellSlots { get; private set; } =
        new(new Dictionary<(int, int), List<SlotUsage>>());

    public KrizaljkaTemplate Template { get; }
    public KrizaljkaSolveState State { get; }

    public IReadOnlyList<AssignedTerm> AssignedTerms => State.AssignedTermsBySlotId.Values.ToList().AsReadOnly();
    

    private TheKrizaljka(
        KrizaljkaTemplate template,
        KrizaljkaSolveState? state)
    {
        Template = template;
        State = state ?? new KrizaljkaSolveState();
    }

    private void Init()
    {
        var analysis = new KrizaljkaAnalyzer().GeTemplateAnalysis(Template);
        Slots = analysis.Slots;
        Intersections = analysis.Intersections;
        CellSlots = analysis.CellSlots.AsReadOnly();
    }

    public static TheKrizaljka Create(
        KrizaljkaTemplate template,
        KrizaljkaSolveState? state)
    {
        var krizaljka = new TheKrizaljka(template, state);
        krizaljka.Init();
        return krizaljka;
    }
}
