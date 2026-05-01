namespace Krizaljka.Domain.User.Models;

public record AppUserMe(
    long Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? DisplayName);