
namespace Krizaljka.Domain.Idea;

public record KrizaljkaIdeaListItem(
    string Id,
    KrizaljkaIdeaStatus Status,
    string ThemeName,
    long CreatedById,
    DateTimeOffset CreatedOn,
    string Changestamp);
