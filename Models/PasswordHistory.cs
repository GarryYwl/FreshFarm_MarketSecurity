using System.ComponentModel.DataAnnotations;

namespace FreshFarmMarketSecurity.Models
{
    public class PasswordHistory
    {
        public int Id { get; set; }

        [Required]
        public int UserAccountId { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation (optional, but nice)
        public UserAccount? UserAccount { get; set; }
    }
}
