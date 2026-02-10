using System.ComponentModel.DataAnnotations;
using FreshFarmMarketSecurity.Data;
using FreshFarmMarketSecurity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreshFarmMarketSecurity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly FreshFarmDbContext _db;
        private readonly PasswordService _pwd;
        private readonly TokenHashService _tokenHash;

        public ResetPasswordModel(FreshFarmDbContext db, PasswordService pwd, TokenHashService tokenHash)
        {
            _db = db;
            _pwd = pwd;
            _tokenHash = tokenHash;
        }

        [BindProperty, Required]
        public string Token { get; set; } = "";

        [BindProperty, Required, DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [BindProperty, Required, DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";

        public string? Error { get; set; }

        public IActionResult OnGet(string token)
        {
            Token = token;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var pwCheck = _pwd.Validate(NewPassword);
            if (!pwCheck.ok)
            {
                ModelState.AddModelError(nameof(NewPassword), pwCheck.reason);
                return Page();
            }

            var tokenHash = _tokenHash.Hash(Token);

            var reset = await _db.PasswordResetTokens
                .Where(r => r.TokenHash == tokenHash)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            if (reset == null || reset.UsedAt != null || reset.ExpiresAt < DateTimeOffset.UtcNow)
            {
                Error = "Reset link is invalid or expired.";
                return Page();
            }

            var user = await _db.UserAccounts.SingleOrDefaultAsync(u => u.Id == reset.UserAccountId);
            if (user == null)
            {
                Error = "Reset link is invalid or expired.";
                return Page();
            }

            // Invalidate sessions
            user.CurrentSessionToken = null;
            user.CurrentSessionIssuedAt = null;

            user.PasswordHash = _pwd.Hash(NewPassword);
            user.LastPasswordChangedAt = DateTimeOffset.UtcNow;

            reset.UsedAt = DateTimeOffset.UtcNow;

            _db.AuditLogs.Add(new Models.AuditLog { Email = user.Email, Action = "PASSWORD_RESET_SUCCESS" });

            await _db.SaveChangesAsync();

            TempData["Info"] = "Password reset successful. Please log in.";
            return RedirectToPage("/Account/Login");
        }
    }
}
