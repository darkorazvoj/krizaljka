using System.Collections;

namespace Krizaljka.Domain.Creator;

public sealed class SlotDomain(BitArray candidates, int count)
{
    public BitArray Candidates { get; } = candidates;

    public int Count { get; set; } = count;

    public SlotDomain Clone()
    {
        return new SlotDomain(new BitArray(Candidates), Count);
    }
}
