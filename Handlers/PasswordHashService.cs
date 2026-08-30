using Microsoft.AspNetCore.Identity;
using smp_trask1.Models;

namespace smp_trask1.Handlers;

public class PasswordHashService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        return _passwordHasher.HashPassword(null!, password);
    }

    public bool VerifyPassword(string passwordHash, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) ||
            string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            null!,
            passwordHash,
            providedPassword);

        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
