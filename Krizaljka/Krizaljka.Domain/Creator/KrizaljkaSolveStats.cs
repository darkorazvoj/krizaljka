namespace Krizaljka.Domain.Creator;

public sealed class KrizaljkaSolveStats
{
    public bool Solved { get; init; }
    public long ElapsedMilliseconds { get; init; }
    public long RecursiveCalls { get; init; }
    public long CandidateTries { get; init; }
    public long Backtracks { get; init; }
    public long DeadEnds { get; init; }
    public long FullyFilledAutoAssignments { get; init; }
    public long SingletonAutoAssignments { get; init; }
    public int MaxAssignedSlotsReached { get; init; }
    public int FinalAssignedSlots { get; init; }
}
