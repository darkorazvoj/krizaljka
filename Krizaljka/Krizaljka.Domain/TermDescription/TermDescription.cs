namespace Krizaljka.Domain.TermDescription;

public record TermDescription(
    long Id,
    long TermId,
    string Description,
    long BatchId,
    long CreatedById,
    DateTimeOffset? CreatedOn,
    string Changestamp);
