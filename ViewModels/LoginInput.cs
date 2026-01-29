using System.ComponentModel.DataAnnotations;

namespace FreshFarmMarketSecurity.ViewModels
{
    public class LoginInput
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
