namespace Krizaljka.WebApi.Models.Term;

public record InsertTermRequest(int? LanguageId, string? Description, string? Term);
