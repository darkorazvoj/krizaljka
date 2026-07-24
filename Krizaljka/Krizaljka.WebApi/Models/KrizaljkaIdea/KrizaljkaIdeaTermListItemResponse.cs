using Krizaljka.Domain.Idea;

namespace Krizaljka.WebApi.Models.KrizaljkaIdea;

public record KrizaljkaIdeaTermListItemResponse(
    TermItemType TermType,
    string Id,
    long? TermId,
    long TermArrayId,
    string? TermRawValue,
    int? TermLength,
    bool? TermIsActive);
