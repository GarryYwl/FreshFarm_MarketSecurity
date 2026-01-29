using FreshFarmMarketSecurity.Data;
using FreshFarmMarketSecurity.Models;
using FreshFarmMarketSecurity.Services;
using FreshFarmMarketSecurity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Encodings.Web;

namespace FreshFarmMarketSecurity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly FreshFarmDbContext _db;
        private readonly EncryptionService _enc;
        private readonly PasswordService _pwd;
        private readonly HtmlEncoder _html;
        private readonly ReCaptchaService _captcha;
        private readonly IConfiguration _config;

        public RegisterModel(FreshFarmDbContext db, EncryptionService enc, PasswordService pwd, HtmlEncoder html, ReCaptchaService captcha, IConfiguration config)
        {
            _db = db;
            _enc = enc;
            _pwd = pwd;
            _html = html;
            _captcha = captcha;
            _config = config;
            ReCaptchaSiteKey = _config["GoogleReCaptcha:SiteKey"] ?? "";
        }


        [BindProperty]
        public RegisterInput Input { get; set; } = new();

        public string? PasswordFeedback { get; set; }

        [BindProperty]
        public string? RecaptchaToken { get; set; }
        public string ReCaptchaSiteKey { get; private set; } = "";



        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var (okCaptcha, reason, score) = await _captcha.VerifyAsync(RecaptchaToken ?? "", "register");
            if (!okCaptcha)
            {
                ModelState.AddModelError(string.Empty, $"reCAPTCHA failed: {reason}");
                return Page();
            }

            // Server-side password complexity (required)
            var (ok, msg) = _pwd.CheckComplexity(Input.Password);
            PasswordFeedback = msg;
            if (!ok)
            {
                ModelState.AddModelError("Input.Password", msg);
                return Page();
            }

            // Duplicate email check (required)
            var exists = await _db.UserAccounts.AnyAsync(u => u.Email == Input.Email);
            if (exists)
            {
                ModelState.AddModelError("Input.Email", "Email already exists. Please use another email.");
                return Page();
            }

            // Validate photo is JPG (required)
            var ext = Path.GetExtension(Input.Photo.FileName).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg")
            {
                ModelState.AddModelError("Input.Photo", "Photo must be a .JPG file.");
                return Page();
            }

            // Save photo into wwwroot/uploads
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsDir);

            var safeFile = $"{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(uploadsDir, safeFile);

            using (var fs = new FileStream(savePath, FileMode.Create))
            {
                await Input.Photo.CopyToAsync(fs);
            }

            // Encode AboutMe to reduce XSS risk before saving
            var aboutMeEncoded = Input.AboutMe is null ? null : WebUtility.HtmlEncode(Input.AboutMe);

            var user = new UserAccount
            {
                FullName = Input.FullName,
                Email = Input.Email,
                PasswordHash = _pwd.Hash(Input.Password),
                CreditCardEncrypted = _enc.Encrypt(Input.CreditCardNo),
                Gender = Input.Gender,
                MobileNo = Input.MobileNo,
                DeliveryAddress = Input.DeliveryAddress,
                PhotoPath = "/uploads/" + safeFile,
                AboutMe = aboutMeEncoded
            };

            _db.UserAccounts.Add(user);

            // Audit log
            _db.AuditLogs.Add(new AuditLog
            {
                Email = Input.Email,
                Action = "REGISTER",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });

            await _db.SaveChangesAsync();

            return RedirectToPage("/Account/Login");
        }
    }
}
