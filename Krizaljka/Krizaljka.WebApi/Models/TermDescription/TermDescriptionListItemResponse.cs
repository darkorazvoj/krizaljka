namespace Krizaljka.WebApi.Models.TermDescription;

public record TermDescriptionListItemResponse(
    long Id,
    long TermId,
    string Description,
    long BatchId,
    long CreatedById,
    string Changestamp);
