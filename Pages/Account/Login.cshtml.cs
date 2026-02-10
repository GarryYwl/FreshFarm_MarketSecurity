using FreshFarmMarketSecurity.Data;
using FreshFarmMarketSecurity.Models;
using FreshFarmMarketSecurity.Services;
using FreshFarmMarketSecurity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreshFarmMarketSecurity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly FreshFarmDbContext _db;
        private readonly PasswordService _pwd;

        // Tweak these constants if you want
        private const int MaxAttempts = 3;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);
        private readonly ReCaptchaService _captcha;
        private readonly IConfiguration _config;

        public LoginModel(FreshFarmDbContext db, PasswordService pwd, ReCaptchaService captcha, IConfiguration config)
        {
            _db = db;
            _pwd = pwd;
            _captcha = captcha;
            _config = config;
            ReCaptchaSiteKey = _config["GoogleReCaptcha:SiteKey"] ?? "";
        }



        [BindProperty]
        public LoginInput Input { get; set; } = new();

        public string? Message { get; set; }
        
        [BindProperty]
        public string? RecaptchaToken { get; set; }

        public string ReCaptchaSiteKey { get; private set; } = "";


        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var (okCaptcha, reason, score) = await _captcha.VerifyAsync(RecaptchaToken ?? "", "login");
            if (!okCaptcha)
            {
                Message = $"reCAPTCHA failed: {reason}";
                await AddAuditAsync(Input.Email, "LOGIN_BLOCKED_RECAPTCHA");
                await _db.SaveChangesAsync();
                return Page();
            }

            var user = await _db.UserAccounts.SingleOrDefaultAsync(u => u.Email == Input.Email);

            // Avoid user enumeration: use a generic message if user not found
            if (user is null)
            {
                await AddAuditAsync(Input.Email, "LOGIN_FAIL_NOUSER");
                Message = "Invalid email or password.";
                return Page();
            }

            // Lockout check
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                await AddAuditAsync(user.Email, "LOGIN_BLOCKED_LOCKED");
                Message = $"Account locked. Try again later.";
                return Page();
            }

            // Verify password hash
            var ok = _pwd.Verify(user.PasswordHash, Input.Password);
            if (!ok)
            {
                user.FailedLoginAttempts++;

                await AddAuditAsync(user.Email, "LOGIN_FAIL_BADPWD");

                // Lock account after 3 failures
                if (user.FailedLoginAttempts >= MaxAttempts)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.Add(LockoutDuration);
                    user.FailedLoginAttempts = 0; // reset counter after lockout
                    await AddAuditAsync(user.Email, "ACCOUNT_LOCKED");
                }

                await _db.SaveChangesAsync();
                Message = "Invalid email or password.";
                return Page();
            }

            // Successful login: clear lockout + reset attempts
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            // Create a fresh session token and store in DB for multi-login detection
            var sessionToken = Guid.NewGuid().ToString("N");
            user.CurrentSessionToken = sessionToken;
            user.CurrentSessionIssuedAt = DateTimeOffset.UtcNow;

            // Save session
            HttpContext.Session.SetString("AuthEmail", user.Email);
            HttpContext.Session.SetString("AuthToken", sessionToken);
            HttpContext.Session.SetInt32("AuthUserId", user.Id);

            await AddAuditAsync(user.Email, "LOGIN_SUCCESS");

            await _db.SaveChangesAsync();

            // Max password age check (before redirect)
            int maxAgeDays = _config.GetValue<int>("Security:MaxPasswordAgeDays", 90);

            var lastChanged = user.LastPasswordChangedAt ?? DateTimeOffset.MinValue;
            bool isExpired = (DateTimeOffset.UtcNow - lastChanged) > TimeSpan.FromDays(maxAgeDays);

            if (isExpired)
            {
                // allow login session but force password change first
                HttpContext.Session.SetString("ForcePasswordChange", "1");
                TempData["Info"] = $"Your password is older than {maxAgeDays} days. Please change it to continue.";
                return RedirectToPage("/Account/ChangePassword");
            }

            // Normal flow
            return RedirectToPage("/Home/Index");


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

            // Note: don't SaveChanges here to avoid extra transactions;
            // caller will SaveChangesAsync at end.
            await Task.CompletedTask;
        }
    }
}
