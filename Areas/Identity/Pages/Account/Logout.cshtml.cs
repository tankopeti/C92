using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.SignalR;
using Cloud9_2.Hubs;
using System.Threading.Tasks;
using System.Linq;

namespace Cloud9._2.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<UserActivityHub> _hubContext;

        public LogoutModel(
            SignInManager<ApplicationUser> signInManager,
            ILogger<LogoutModel> logger,
            ApplicationDbContext context,
            IHubContext<UserActivityHub> hubContext)
        {
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // Find the user's active session
            var user = await _signInManager.UserManager.GetUserAsync(User);
            if (user != null)
            {
                var activeSession = _context.UserActivities
                    .Where(a => a.UserId == user.Id && a.IsActive)
                    .OrderByDescending(a => a.LoginTime)
                    .FirstOrDefault();

                if (activeSession != null)
                {
                    activeSession.LogoutTime = DateTime.UtcNow;
                    activeSession.IsActive = false;
                    _context.UserActivities.Update(activeSession);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserName} logged out.", user.UserName);

                    // Notify connected clients
                    var activeUsers = _context.UserActivities
                        .Where(a => a.IsActive)
                        .Select(a => new { a.UserName, LoginTime = a.LoginTime.ToString("yyyy-MM-dd HH:mm:ss") })
                        .ToList();
                    await _hubContext.Clients.All.SendAsync("ReceiveActiveUsers", activeUsers);
                }
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User signed out.");

            returnUrl ??= Url.Content("~/");
            return LocalRedirect(returnUrl);
        }
    }
}