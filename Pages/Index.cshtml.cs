using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FreshFarmMarketSecurity.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            // You can change this to /Home/Index if you prefer
            return RedirectToPage("/Account/Login");
        }
    }
}
