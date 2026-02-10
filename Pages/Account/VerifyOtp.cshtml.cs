using System.ComponentModel.DataAnnotations;
using FreshFarmMarketSecurity.Data;
using FreshFarmMarketSecurity.Models;
using FreshFarmMarketSecurity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreshFarmMarketSecurity.Pages.Account
{
    public class VerifyOtpModel : PageModel
    {
        private readonly FreshFarmDbContext _db;
        private readonly IConfiguration _config;
        private readonly TokenHashService _tokenHash;

        public VerifyOtpModel(FreshFarmDbContext db, TokenHashService tokenHash, IConfiguration config)
        {
            _db = db;
            _tokenHash = tokenHash;
            _config = config;
        }

        [BindProperty, Required]
        public string Otp { get; set; } = "";

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var userId = HttpContext.Session.GetInt32("Pending2FAUserId");
            if (userId == null)
            {
                TempData["Info"] = "2FA session expired. Please log in again.";
                return RedirectToPage("/Account/Login");
            }

            var user = await _db.UserAccounts.SingleOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null)
                return RedirectToPage("/Account/Login");

            if (user.TwoFactorOtpExpiresAt == null || user.TwoFactorOtpExpiresAt < DateTimeOffset.UtcNow)
            {
                HttpContext.Session.Remove("Pending2FAUserId");
                TempData["Info"] = "OTP expired. Please log in again.";
                return RedirectToPage("/Account/Login");
            }

            user.TwoFactorOtpAttempts++;
            if (user.TwoFactorOtpAttempts > 5)
            {
                // lock out 2FA attempts
                user.TwoFactorOtpHash = null;
                user.TwoFactorOtpExpiresAt = null;
                HttpContext.Session.Remove("Pending2FAUserId");
                _db.AuditLogs.Add(new AuditLog { Email = user.Email, Action = "2FA_OTP_TOO_MANY_ATTEMPTS" });
                await _db.SaveChangesAsync();
                TempData["Info"] = "Too many OTP attempts. Please log in again.";
                return RedirectToPage("/Account/Login");
            }

            var otpHash = _tokenHash.Hash(Otp);
            if (user.TwoFactorOtpHash != otpHash)
            {
                _db.AuditLogs.Add(new AuditLog { Email = user.Email, Action = "2FA_OTP_FAIL" });
                await _db.SaveChangesAsync();
                ModelState.AddModelError(string.Empty, "Invalid OTP.");
                return Page();
            }

            // Max password age check AFTER OTP success
            int maxAgeDays = _config.GetValue<int>("Security:MaxPasswordAgeDays", 90);
            if (maxAgeDays < 1) maxAgeDays = 1;
            var lastChanged = user.LastPasswordChangedAt ?? DateTimeOffset.MinValue;
            var age = DateTimeOffset.UtcNow - lastChanged;
            bool isExpired = age > TimeSpan.FromDays(maxAgeDays);

            if (isExpired)
            {
                // Force change password before allowing access
                HttpContext.Session.SetString("ForcePasswordChange", "1");

                // Create a normal authenticated session now (so ChangePassword can work)
                var sessionTokenExpired = Guid.NewGuid().ToString("N");
                user.CurrentSessionToken = sessionTokenExpired;
                user.CurrentSessionIssuedAt = DateTimeOffset.UtcNow;

                HttpContext.Session.Remove("Pending2FAUserId");
                HttpContext.Session.SetString("AuthEmail", user.Email);
                HttpContext.Session.SetString("AuthToken", sessionTokenExpired);
                HttpContext.Session.SetInt32("AuthUserId", user.Id);

                TempData["Info"] = $"Your password is older than {maxAgeDays} days. Please change it to continue.";
                _db.AuditLogs.Add(new AuditLog { Email = user.Email, Action = "LOGIN_FORCE_PW_CHANGE_MAX_AGE" });

                await _db.SaveChangesAsync();
                return RedirectToPage("/Account/ChangePassword");
            }

            // OTP success: clear OTP + complete login 
            user.TwoFactorOtpHash = null;
            user.TwoFactorOtpExpiresAt = null;
            user.TwoFactorOtpAttempts = 0;

            var sessionToken = Guid.NewGuid().ToString("N");
            user.CurrentSessionToken = sessionToken;
            user.CurrentSessionIssuedAt = DateTimeOffset.UtcNow;

            HttpContext.Session.Remove("Pending2FAUserId");
            HttpContext.Session.SetString("AuthEmail", user.Email);
            HttpContext.Session.SetString("AuthToken", sessionToken);
            HttpContext.Session.SetInt32("AuthUserId", user.Id);

            _db.AuditLogs.Add(new AuditLog { Email = user.Email, Action = "2FA_OTP_SUCCESS" });
            await _db.SaveChangesAsync();


            return RedirectToPage("/Home/Index");
        }
    }
}
