
namespace Krizaljka.Domain.Idea;

public record KrizaljkaIdeaAllTemplatesListItem(
    string Id,
    long? TemplateId,
    string? Name,
    bool? IsActive);
