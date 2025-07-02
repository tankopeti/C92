using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cloud9_2.Models;

namespace Cloud9_2.Pages
{
    public class ChatModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public ApplicationUser CurrentUser { get; set; }
        public List<ApplicationUser> OtherUsers { get; set; }

        public async Task OnGetAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            OtherUsers = _userManager.Users
                .Where(u => u.UserName != CurrentUser.UserName)
                .ToList();
        }
    }
}
