
namespace Krizaljka.WebApi.Models.KrizaljkaIdea;

public record KrizaljkaIdeaAllTemplatesListItemResponse(
    string Id,
    long? TemplateId,
    string? Name,
    bool? IsActive);
