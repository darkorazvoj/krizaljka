using Krizaljka.Domain.Idea;

namespace Krizaljka.WebApi.Models.KrizaljkaIdea;

public record KrizaljkaIdeaTemplateListItemResponse(
    TemplateItemType TemplateType,
    string Id,
    long TemplateArrayId,
    long? TemplateId);
