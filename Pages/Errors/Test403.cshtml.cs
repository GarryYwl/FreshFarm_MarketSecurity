using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FreshFarmMarketSecurity.Pages.Errors
{
    public class Test403Model : PageModel
    {
        public IActionResult OnGet()
        {
            return StatusCode(403);
        }
    }
}
