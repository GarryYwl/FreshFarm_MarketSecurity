using FreshFarmMarketSecurity.Data;
using FreshFarmMarketSecurity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreshFarmMarketSecurity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly FreshFarmDbContext _db;

        public LogoutModel(FreshFarmDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var email = HttpContext.Session.GetString("AuthEmail");

            if (!string.IsNullOrEmpty(email))
            {
                var user = await _db.UserAccounts.SingleOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    // Invalidate session token in DB
                    user.CurrentSessionToken = null;
                    user.CurrentSessionIssuedAt = null;

                    _db.AuditLogs.Add(new AuditLog
                    {
                        Email = email,
                        Action = "LOGOUT",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers.UserAgent.ToString()
                    });

                    await _db.SaveChangesAsync();
                }
            }

            HttpContext.Session.Clear();
            return RedirectToPage("/Account/Login");
        }
    }
}
