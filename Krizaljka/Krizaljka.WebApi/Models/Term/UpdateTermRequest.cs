
namespace Krizaljka.WebApi.Models.Term;

public record UpdateTermRequest(
    int? LanguageId,
    string? Description,
    string? Term,
    string? Changestamp);
