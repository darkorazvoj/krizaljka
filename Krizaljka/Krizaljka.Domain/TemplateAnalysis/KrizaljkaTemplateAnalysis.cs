using Krizaljka.Domain.Template;

namespace Krizaljka.Domain.TemplateAnalysis;

public record KrizaljkaTemplateAnalysis(
    long Id,
    KrizaljkaTemplate Template,
    IReadOnlyList<KrizaljkaSlot> Slots,
    IReadOnlyList<KrizaljkaIntersection> Intersections,
    Dictionary<(int, int), List<SlotUsage>> CellSlots);
