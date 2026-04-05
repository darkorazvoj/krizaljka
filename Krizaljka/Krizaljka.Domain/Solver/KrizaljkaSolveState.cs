
using Krizaljka.Domain.Extensions;
using System.Text.Json.Serialization;

namespace Krizaljka.Domain.Solver;

public sealed class KrizaljkaSolveState
{
    public Dictionary<int, AssignedTerm> AssignedTermsBySlotId { get; init; } = [];
    public HashSet<long> UsedTermsIds { get; init; } = [];

    [JsonIgnore]
    public Dictionary<(int Row, int Col), string> LettersByCell { get; private set; } = [];

    [JsonPropertyName("LettersByCell")] // The JSON uses this
    public Dictionary<string, string> LettersByCellSerializable
    {
        get => LettersByCell.ToDictionary(
            k => $"{k.Key.Row}_{k.Key.Col}",
            v => v.Value);

        set => LettersByCell = value.ToDictionary(
            k =>
            {
                var parts = k.Key.Split('_');
                return (int.Parse(parts[0]), int.Parse(parts[1]));
            },
            v => v.Value.NormalizeLetters());
    }

    public bool IsAssigned(int slotId) => AssignedTermsBySlotId.ContainsKey(slotId);


    public bool ClearSlot(int slotId)
    {
        if (!AssignedTermsBySlotId.Remove(slotId, out var term))
        {
            return false;
        }

        UsedTermsIds.Remove(term.TermId);
        return true;
    }
}
