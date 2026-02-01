using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;

namespace FreshFarmMarketSecurity.Services
{
    public class PasswordService
    {
        private readonly PasswordHasher<string> _hasher = new();

        public string Hash(string password) => _hasher.HashPassword("FreshFarmMarketUser", password);

        public bool Verify(string hashed, string provided)
            => _hasher.VerifyHashedPassword("FreshFarmMarketUser", hashed, provided) != PasswordVerificationResult.Failed;

        public (bool ok, string message) CheckComplexity(string password)
        {
            if (password.Length < 12) return (false, "Password must be at least 12 characters.");
            if (!Regex.IsMatch(password, "[a-z]")) return (false, "Password must include a lowercase letter.");
            if (!Regex.IsMatch(password, "[A-Z]")) return (false, "Password must include an uppercase letter.");
            if (!Regex.IsMatch(password, "[0-9]")) return (false, "Password must include a number.");
            if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]")) return (false, "Password must include a special character.");

            return (true, "Strong password.");
        }

        public (bool ok, string reason) Validate(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required.");

            if (password.Length < 12)
                return (false, "Password must be at least 12 characters.");

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            if (!hasUpper) return (false, "Password must contain at least one uppercase letter.");
            if (!hasLower) return (false, "Password must contain at least one lowercase letter.");
            if (!hasDigit) return (false, "Password must contain at least one number.");
            if (!hasSpecial) return (false, "Password must contain at least one special character.");

            return (true, "OK");
        }
    }
}
