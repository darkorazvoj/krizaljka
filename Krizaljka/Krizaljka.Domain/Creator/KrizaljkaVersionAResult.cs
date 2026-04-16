using Krizaljka.Domain.Template;

namespace Krizaljka.Domain.Creator;

public record KrizaljkaVersionAResult(
    bool Solved,
    KrizaljkaTemplate? Template,
    IReadOnlyList<KrizaljkaThemePlacement> ThemePlacements,
    KrizaljkaCreateResult? CreateResult);
