using Krizaljka.Domain.Terms;

namespace Krizaljka.WebApi.Models.Term;

public record TermListItemResponse(
    long Id,
    TermLanguage LanguageId,
    string RawValue,
    int Length,
    bool IsActive);
