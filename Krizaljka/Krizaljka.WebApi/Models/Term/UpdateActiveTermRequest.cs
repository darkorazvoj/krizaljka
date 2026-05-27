namespace Krizaljka.WebApi.Models.Term;

public record UpdateActiveTermRequest(bool? IsActive, string? Changestamp);
