using Microsoft.AspNetCore.Authorization; // For [Authorize]
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloud9_2.Models; // For ApplicationUser and UserActivity
using Cloud9_2.Data; // Adjust to match ApplicationDbContext's namespace

namespace Cloud9_2.Pages.Admin
{

    public class ActiveUsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ActiveUsersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<UserActivity> ActiveUsers { get; set; }

        public void OnGet()
        {
            ActiveUsers = _context.UserActivities
                            .Where(a => a.IsActive)
                            .OrderBy(a => a.LoginTime)
                            .ToList();
        }
    }
}