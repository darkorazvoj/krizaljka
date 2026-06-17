
namespace Krizaljka.Domain.TermDescription;

public record TermDescriptionListItem(
    long Id,
    long TermId,
    string Description,
    long BatchId,
    long CreatedById);
