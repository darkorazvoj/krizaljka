using Krizaljka.Domain.Template;

namespace Krizaljka.Domain.Creator;

public record KrizaljkaVersionARequest(
    IReadOnlyList<KrizaljkaTemplateBasic> Templates,
    IReadOnlyList<long> ThemeTermIds,
    int MaxTemplatesToTry = 10,
    int MaxLayoutsPerTemplate = 20,
    int MaxSlotsPerThemeTerm = 12,
    int MaxParallelTemplates = 5,
    int MaxSolveMinutesPerTemplate = 60,
    int? StopAfterSolvedTemplates = null);
