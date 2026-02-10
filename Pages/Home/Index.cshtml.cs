using FreshFarmMarketSecurity.Data;
using FreshFarmMarketSecurity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreshFarmMarketSecurity.Pages.Home
{
    public class IndexModel : PageModel
    {
        private readonly FreshFarmDbContext _db;
        private readonly EncryptionService _enc;

        public IndexModel(FreshFarmDbContext db, EncryptionService enc)
        {
            _db = db;
            _enc = enc;
        }

        // Data to display
        public string FullName { get; private set; } = "";
        public string Email { get; private set; } = "";
        public string Gender { get; private set; } = "";
        public string MobileNo { get; private set; } = "";
        public string DeliveryAddress { get; private set; } = "";
        public string PhotoPath { get; private set; } = "";
        public string? AboutMe { get; private set; }

        public string DecryptedCreditCard { get; private set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            var email = HttpContext.Session.GetString("AuthEmail");
            var token = HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToPage("/Account/Login");

            var user = await _db.UserAccounts.SingleOrDefaultAsync(u => u.Email == email);
            if (user is null || user.CurrentSessionToken != token)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Account/Login");
            }

            // Now enforce forced password change
            if (HttpContext.Session.GetString("ForcePasswordChange") == "1")
            {
                return RedirectToPage("/Account/ChangePassword");
            }

            // Populate view data
            FullName = user.FullName;
            Email = user.Email;
            Gender = user.Gender;
            MobileNo = user.MobileNo;
            DeliveryAddress = user.DeliveryAddress;
            PhotoPath = user.PhotoPath;
            AboutMe = user.AboutMe;

            // Decrypt credit card for display (assignment requirement)
            try
            {
                DecryptedCreditCard = _enc.Decrypt(user.CreditCardEncrypted);
            }
            catch
            {
                // If decrypt fails, don't crash demo
                DecryptedCreditCard = "[Unable to decrypt]";
            }

            return Page();
        }
    }
}
