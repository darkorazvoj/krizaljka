using Krizaljka.Domain.Template;

namespace Krizaljka.Domain.Creator;

public record KrizaljkaVersionAResult(
    bool Solved,
    KrizaljkaTemplateBasic? Template,
    IReadOnlyList<KrizaljkaThemePlacement> ThemePlacements,
    KrizaljkaCreateResult? CreateResult);
