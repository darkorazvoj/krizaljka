
namespace Krizaljka.Domain.Terms;

public record TermListItem(
    long Id,
    TermLanguage LanguageId,
    string RawValue,
    int Length,
    bool IsActive,
    long CreatedById);
