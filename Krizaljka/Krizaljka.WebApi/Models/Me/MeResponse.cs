namespace Krizaljka.WebApi.Models.Me;

public record MeResponse(
    long Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? DisplayName);
