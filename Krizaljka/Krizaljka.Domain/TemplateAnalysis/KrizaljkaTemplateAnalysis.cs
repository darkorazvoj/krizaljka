using Krizaljka.Domain.Template;

namespace Krizaljka.Domain.TemplateAnalysis;

public record KrizaljkaTemplateAnalysis(
    KrizaljkaTemplateBasic TemplateBasic,
    IReadOnlyList<KrizaljkaSlot> Slots,
    IReadOnlyList<KrizaljkaIntersection> Intersections,
    IReadOnlyDictionary<int, IReadOnlyList<KrizaljkaIntersection>> IntersectionsBySlotId,
    IReadOnlyDictionary<int, IReadOnlyList<int>> NeighborSlotsIdsBySlotId,
    Dictionary<(int, int), List<SlotUsage>> CellSlots);
