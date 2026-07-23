

using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Idea;

public record KrizaljkaIdeaTermListItem(
    TermItemType TermType,
    string Id,
    long TermId,
    TermLanguage TermLanguageId,
    int TermLength,
    bool TermIsActive);
