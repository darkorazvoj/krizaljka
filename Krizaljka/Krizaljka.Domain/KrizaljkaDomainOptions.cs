

namespace Krizaljka.Domain;

public sealed class KrizaljkaDomainOptions
{
    public int MaxFailedLoginAttempts { get; set; } = 6;
    public int CoolOffTimeInMinutes { get; set; } = 7;
    public int MaxSolveMinutesPerTemplate { get; set; } = 5;
    public int MaxTemplatesToTry { get; set; } = 100;
    public int MaxLayoutsPerTemplate { get; set; } = 20;
    public int MaxSlotsPerThemeTerm { get; set; } = 12;
    public int MaxParallelTemplates { get; set; } = 10;
    public int StopAfterSolvedTemplates { get; set; } = 1;
}
