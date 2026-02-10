using System.ComponentModel.DataAnnotations;

namespace FreshFarmMarketSecurity.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }

        [Required]
        public int UserAccountId { get; set; }

        [Required]
        public string TokenHash { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? UsedAt { get; set; }
    }
}
