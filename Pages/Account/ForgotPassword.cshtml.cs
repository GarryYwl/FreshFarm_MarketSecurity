using System.ComponentModel.DataAnnotations;
using FreshFarmMarketSecurity.Data;
using FreshFarmMarketSecurity.Models;
using FreshFarmMarketSecurity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreshFarmMarketSecurity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly FreshFarmDbContext _db;
        private readonly EmailService _email;
        private readonly TokenHashService _tokenHash;

        public ForgotPasswordModel(FreshFarmDbContext db, EmailService email, TokenHashService tokenHash)
        {
            _db = db;
            _email = email;
            _tokenHash = tokenHash;
        }

        [BindProperty, Required, EmailAddress]
        public string Email { get; set; } = "";

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // Always show generic message to prevent user enumeration
            TempData["Info"] = "If the email exists, a reset link has been sent.";

            var user = await _db.UserAccounts.SingleOrDefaultAsync(u => u.Email == Email);
            if (user == null)
            {
                await _db.SaveChangesAsync();
                return RedirectToPage();
            }

            // Create token
            var token = _tokenHash.GenerateSecureToken();
            var tokenHash = _tokenHash.Hash(token);

            var reset = new PasswordResetToken
            {
                UserAccountId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
                UsedAt = null
            };

            _db.PasswordResetTokens.Add(reset);
            _db.AuditLogs.Add(new AuditLog { Email = user.Email, Action = "PASSWORD_RESET_REQUESTED" });

            await _db.SaveChangesAsync();

            // Build link (local)
            var link = Url.Page("/Account/ResetPassword", null, new { token }, Request.Scheme);

            await _email.SendAsync(user.Email, "Fresh Farm Market - Password Reset",
                $"Click to reset your password (valid 15 minutes):\n{link}");

            return RedirectToPage();
        }
    }
}
