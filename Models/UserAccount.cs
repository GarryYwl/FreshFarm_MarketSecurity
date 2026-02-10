using System;
using System.ComponentModel.DataAnnotations;

namespace FreshFarmMarketSecurity.Models
{
    public class UserAccount
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(120)]
        public string Email { get; set; } = string.Empty;

        // Store only a hash, never plaintext password
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Store encrypted value (we will encrypt/decrypt via a service)
        [Required]
        public string CreditCardEncrypted { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Gender { get; set; } = string.Empty;

        [Required, Phone, StringLength(30)]
        public string MobileNo { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string DeliveryAddress { get; set; } = string.Empty;

        // Save relative path e.g. /uploads/abc.jpg
        [Required, StringLength(260)]
        public string PhotoPath { get; set; } = string.Empty;

        // Allow all special characters (we will HTML-encode before saving)
        [StringLength(2000)]
        public string? AboutMe { get; set; }

        // Login protection
        public int FailedLoginAttempts { get; set; } = 0;

        public DateTimeOffset? LockoutEnd { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string? CurrentSessionToken { get; set; }

        public DateTimeOffset? CurrentSessionIssuedAt { get; set; }

        public DateTimeOffset? LastPasswordChangedAt { get; set; }

        public string? TwoFactorOtpHash { get; set; }
        
        public DateTimeOffset? TwoFactorOtpExpiresAt { get; set; }
        
        public int TwoFactorOtpAttempts { get; set; }
    }
}
