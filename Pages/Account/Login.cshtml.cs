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
        private readonly EmailService _email;
        private readonly TokenHashService _tokenHash;


        // Tweak these constants if you want
        private const int MaxAttempts = 3;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(1);
        private readonly ReCaptchaService _captcha;
        private readonly IConfiguration _config;

        public LoginModel(
            FreshFarmDbContext db,
            PasswordService pwd,
            ReCaptchaService captcha,
            IConfiguration config,
            EmailService email,
            TokenHashService tokenHash)
        {
            _db = db;
            _pwd = pwd;
            _captcha = captcha;
            _config = config;
            _email = email;
            _tokenHash = tokenHash;

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

            // Successful password: clear lockout + reset attempts
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            // 2FA email OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var otpHash = _tokenHash.Hash(otp);

            user.TwoFactorOtpHash = otpHash;
            user.TwoFactorOtpExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
            user.TwoFactorOtpAttempts = 0;

            // Pending session (not fully logged in)
            HttpContext.Session.SetInt32("Pending2FAUserId", user.Id);

            _db.AuditLogs.Add(new AuditLog { Email = user.Email, Action = "2FA_OTP_SENT" });
            await _db.SaveChangesAsync();

            await _email.SendAsync(
                user.Email,
                "Fresh Farm Market - Your OTP",
                $"Your OTP is: {otp}\nIt expires in 5 minutes.");

            return RedirectToPage("/Account/VerifyOtp");
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
