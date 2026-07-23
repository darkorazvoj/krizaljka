using Krizaljka.Domain.Idea;
using Krizaljka.Domain.Terms;

namespace Krizaljka.WebApi.Models.KrizaljkaIdea;

public record KrizaljkaIdeaTermListItemResponse(
    TermItemType TermType,
    string Id,
    long TermId,
    TermLanguage TermLanguageId,
    int TermLength,
    bool TermIsActive);
