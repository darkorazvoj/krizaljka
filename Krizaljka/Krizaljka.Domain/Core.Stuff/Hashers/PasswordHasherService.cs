using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.AspNetCore.Identity;

namespace Krizaljka.Domain.Core.Stuff.Hashers;

internal class PasswordHasherService: IPasswordHasherService
{
    private readonly PasswordHasher<string> _hasher = new();

    public string HashPassword(string password) =>
        string.IsNullOrWhiteSpace(password)
            ? throw new ArgumentException("Password is required", nameof(password))
            : _hasher.HashPassword(user: "", password);

    public IServiceResult VerifyHashedPassword(string passwordHash, string password)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) ||
            string.IsNullOrWhiteSpace(password))
        {
            return new Error(string.Empty);
        }

        PasswordVerificationResult? result;
        try
        {
            result = _hasher.VerifyHashedPassword(
                user: "", hashedPassword: passwordHash, providedPassword: password);
        }
        catch
        {
            return new Error(string.Empty);
        }

        return result switch
        {
            PasswordVerificationResult.Success => new Success<(bool isOk, bool needsRehash)>((true, false)),
            PasswordVerificationResult.SuccessRehashNeeded => new Success<(bool isOk, bool needsRehash)>((true, true)),
            _ => new Error(string.Empty)
        };
    }
}
