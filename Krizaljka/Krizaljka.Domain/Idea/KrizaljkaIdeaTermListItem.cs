
namespace Krizaljka.Domain.Idea;

public record KrizaljkaIdeaTermListItem(
    TermItemType TermType,
    string Id,
    long? TermId,
    long TermArrayId,
    string? TermRawValue,
    int? TermLength,
    bool? TermIsActive);
