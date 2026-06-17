namespace Krizaljka.WebApi.Models.TermDescription;

public record TermDescriptionResponse(
    long Id,
    long TermId,
    string Description,
    long BatchId,
    long CreatedById,
    DateTimeOffset? CreatedOn,
    string Changestamp);
