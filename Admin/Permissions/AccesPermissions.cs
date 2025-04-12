using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.Admin.AccesPermissions
{
    [Authorize(Roles = "SuperAdmin, Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(ApplicationDbContext context, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }

        public IList<AccessPermission> Permissions { get; set; } = new List<AccessPermission>();
        public IList<IdentityRole> Roles { get; set; } = new List<IdentityRole>();

        [BindProperty]
        public AccessPermission NewPermission { get; set; } = new AccessPermission();

        public async Task OnGetAsync()
        {
            Permissions = await _context.AccessPermissions.ToListAsync() ?? new List<AccessPermission>();
            Roles = await _roleManager.Roles.ToListAsync() ?? new List<IdentityRole>();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Permissions = await _context.AccessPermissions.ToListAsync() ?? new List<AccessPermission>();
                Roles = await _roleManager.Roles.ToListAsync() ?? new List<IdentityRole>();
                return Page();
            }

            if (!string.IsNullOrEmpty(NewPermission.ColumnName))
            {
                NewPermission.CanViewColumn = true;
            }

            _context.AccessPermissions.Add(NewPermission);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var permission = await _context.AccessPermissions.FindAsync(id);
            if (permission != null)
            {
                _context.AccessPermissions.Remove(permission);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}