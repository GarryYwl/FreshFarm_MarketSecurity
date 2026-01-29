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
    }
}
