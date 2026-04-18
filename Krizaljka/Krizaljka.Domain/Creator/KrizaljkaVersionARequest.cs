using Krizaljka.Domain.Template;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Creator;

public record KrizaljkaVersionARequest(
    IReadOnlyList<KrizaljkaTemplate> Templates,
    IReadOnlyList<Term> Terms,
    IReadOnlyList<long> ThemeTermIds,
    int MaxTemplatesToTry = 10,
    int MaxLayoutsPerTemplate = 20,
    int MaxSlotsPerThemeTerm = 12,
    int MaxParallelTemplates = 5,
    int MaxSolveMinutesPerTemplate = 60,
    int? StopAfterSolvedTemplates = null);
