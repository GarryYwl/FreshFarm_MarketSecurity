using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FreshFarmMarketSecurity.ViewModels
{
    public class RegisterInput
    {
        [Required, StringLength(80)]
        public string FullName { get; set; } = string.Empty;

        [Required, CreditCard]
        public string CreditCardNo { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required, Phone]
        public string MobileNo { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        // JPG only - we will enforce in code too
        [Required]
        public IFormFile Photo { get; set; } = default!;

        [StringLength(2000)]
        public string? AboutMe { get; set; }
    }
}
