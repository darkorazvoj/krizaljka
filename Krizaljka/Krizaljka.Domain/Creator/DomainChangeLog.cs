
namespace Krizaljka.Domain.Creator;

public sealed class DomainChangeLog
{
    public List<DomainChange> Changes { get; } = [];
    public HashSet<int> RecordedSlotIds { get; } = [];
}
