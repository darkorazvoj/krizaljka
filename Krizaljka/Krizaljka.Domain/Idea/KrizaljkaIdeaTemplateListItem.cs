
namespace Krizaljka.Domain.Idea;

public record KrizaljkaIdeaTemplateListItem(
    TemplateItemType TemplateType,
    string Id,
    long TemplateArrayId,
    long? TemplateId,
    string? Name,
    bool? IsActive);
