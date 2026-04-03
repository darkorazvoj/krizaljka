
namespace Krizaljka.Domain.Solver;

public sealed class KrizaljkaSolvedState
{
    public Dictionary<int, AssignedTerm> AssignedTermsBySlotId { get; } = [];
    public HashSet<long> UsedTermsIds { get; } = [];
    public Dictionary<(int Row, int Col), string> LettersByCell { get; } = [];

    public bool IsAssigned(int slotId) => AssignedTermsBySlotId.ContainsKey(slotId);
}
