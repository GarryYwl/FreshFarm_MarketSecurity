using System.Security.Cryptography;
using System.Text;

namespace FreshFarmMarketSecurity.Services
{
    public class TokenHashService
    {
        public string Hash(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        public string GenerateSecureToken()
        {
            // 32 bytes => 256-bit token
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
