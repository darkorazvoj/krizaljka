using Krizaljka.Domain.Idea;

namespace Krizaljka.WebApi.Models.KrizaljkaIdea;

public record KrizaljkaIdeaListItemResponse(
    string Id,
    KrizaljkaIdeaStatus Status,
    string ThemeName,
    long CreatedById,
    string Changestamp);
