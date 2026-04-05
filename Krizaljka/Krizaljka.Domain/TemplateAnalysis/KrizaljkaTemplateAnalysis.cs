using Krizaljka.Domain.Template;

namespace Krizaljka.Domain.TemplateAnalysis;

public record KrizaljkaTemplateAnalysis(
    KrizaljkaTemplate Template,
    IReadOnlyList<KrizaljkaSlot> Slots,
    IReadOnlyList<KrizaljkaIntersection> Intersections,
    IReadOnlyDictionary<int, IReadOnlyList<int>> NeighborSlotsIdsBySlotId,
    Dictionary<(int, int), List<SlotUsage>> CellSlots);
