namespace Krizaljka.WebApi.Models.KrizaljkaTemplate;

public record UpdateActiveKrizaljkaTemplateRequest(bool? IsActive, string? Changestamp);
