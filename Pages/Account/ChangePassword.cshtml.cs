using FreshFarmMarketSecurity.Data;
using FreshFarmMarketSecurity.Models;
using FreshFarmMarketSecurity.Services;
using FreshFarmMarketSecurity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreshFarmMarketSecurity.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        private readonly FreshFarmDbContext _db;
        private readonly PasswordService _pwd;

        public ChangePasswordModel(FreshFarmDbContext db, PasswordService pwd)
        {
            _db = db;
            _pwd = pwd;
        }

        [BindProperty]
        public ChangePasswordInput Input { get; set; } = new();

        public string? Message { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Require login
            var email = HttpContext.Session.GetString("AuthEmail");
            var token = HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToPage("/Account/Login");

            // Multi-login detection
            var user = await _db.UserAccounts.SingleOrDefaultAsync(u => u.Email == email);
            if (user is null || user.CurrentSessionToken != token)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Account/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var email = HttpContext.Session.GetString("AuthEmail");
            var token = HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid)
                return Page();

            var user = await _db.UserAccounts.SingleOrDefaultAsync(u => u.Email == email);
            if (user is null || user.CurrentSessionToken != token)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Account/Login");
            }

            // Password min age check MUST be after user is loaded
            if (user.LastPasswordChangedAt.HasValue &&
                DateTimeOffset.UtcNow - user.LastPasswordChangedAt.Value < TimeSpan.FromMinutes(1))
            {
                ModelState.AddModelError(string.Empty, "You can only change your password once per minute.");
                return Page();
            }

            // Verify current password
            if (!_pwd.Verify(user.PasswordHash, Input.CurrentPassword))
            {
                ModelState.AddModelError(string.Empty, "Current password is incorrect.");
                await AddAuditAsync(user.Email, "CHANGE_PASSWORD_FAIL_BAD_CURRENT");
                await _db.SaveChangesAsync();
                return Page();
            }

            // Enforce password rules
            var pwCheck = _pwd.Validate(Input.NewPassword);
            if (!pwCheck.ok)
            {
                ModelState.AddModelError(nameof(Input.NewPassword), pwCheck.reason);
                return Page();
            }

            // Prevent reuse of last 2 passwords + current
            var recentHashes = await _db.PasswordHistories
                .Where(h => h.UserAccountId == user.Id)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => h.PasswordHash)
                .Take(2)
                .ToListAsync();

            recentHashes.Insert(0, user.PasswordHash);

            foreach (var oldHash in recentHashes)
            {
                if (_pwd.Verify(oldHash, Input.NewPassword))
                {
                    // ✅ Put error on ModelState so it shows up in validation summary
                    ModelState.AddModelError(string.Empty, "You cannot reuse your recent passwords.");
                    await AddAuditAsync(user.Email, "CHANGE_PASSWORD_FAIL_REUSE");
                    await _db.SaveChangesAsync();
                    return Page();
                }
            }

            // Save current hash into history
            _db.PasswordHistories.Add(new PasswordHistory
            {
                UserAccountId = user.Id,
                PasswordHash = user.PasswordHash
            });

            // Update password hash
            user.PasswordHash = _pwd.Hash(Input.NewPassword);
            user.LastPasswordChangedAt = DateTimeOffset.UtcNow;

            await AddAuditAsync(user.Email, "CHANGE_PASSWORD_SUCCESS");
            await _db.SaveChangesAsync();

            Message = "Password changed successfully.";
            ModelState.Clear();
            Input = new ChangePasswordInput();
            return Page();
        }

        private async Task AddAuditAsync(string email, string action)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Email = email,
                Action = action,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });
            await Task.CompletedTask;
        }
    }
}
