using System.ComponentModel.DataAnnotations;

namespace FreshFarmMarketSecurity.ViewModels
{
    public class ChangePasswordInput
    {
        [Required, DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
