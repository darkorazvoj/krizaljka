
using System.Text.Json.Serialization;
using Krizaljka.Domain.Extensions;

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


    public KrizaljkaSolveState DeepClone()
    {
        return new KrizaljkaSolveState
        {
            // Dictionaries and HashSets have constructors that take an existing 
            // collection to create a shallow copy of the contents.
            AssignedTermsBySlotId = new Dictionary<int, AssignedTerm>(this.AssignedTermsBySlotId),
            UsedTermsIds = [..UsedTermsIds],
        
            // Since we are creating a new dictionary and filling it with 
            // the same value references, the state is protected.
            LettersByCell = new Dictionary<(int Row, int Col), string>(this.LettersByCell)
        };
    }
}
