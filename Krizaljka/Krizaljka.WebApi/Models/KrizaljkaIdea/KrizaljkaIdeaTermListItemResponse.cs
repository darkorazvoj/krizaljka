using Krizaljka.Domain.Idea;

namespace Krizaljka.WebApi.Models.KrizaljkaIdea;

public record KrizaljkaIdeaTermListItemResponse(
    TermItemType TermType,
    string Id,
    long TermId,
    string TermRawValue,
    int TermLength,
    bool TermIsActive);
