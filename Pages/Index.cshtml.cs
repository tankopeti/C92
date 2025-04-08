using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cloud9_2.Pages
{
    [Authorize(Roles = "SuperAdmin, Admin, User")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public void OnGet()
        {
        }
        
    }
}
