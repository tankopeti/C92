using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.Admin.ColumnVisibilitySettings
{
    [Authorize(Roles = "SuperAdmin, Admin")]
    public class ColumnVisibilitySettingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ColumnVisibilitySettingsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public List<ColumnVisibility> ColumnVisibilities { get; set; }

        public List<string> Pages { get; set; }
        public List<string> Roles { get; set; }

        public async Task OnGetAsync()
        {
            ColumnVisibilities = await _context.ColumnVisibilities.ToListAsync();
            Pages = new List<string> { "/Admin/LoginLog", "/Admin/Users/Index" /* Add more */ };
            Roles = await _context.Roles.Select(r => r.Name).ToListAsync();

            foreach (var pageName in Pages)
            {
                foreach (var role in Roles)
                {
                    var columns = GetColumnsForPage(pageName);
                    foreach (var column in columns)
                    {
                        if (!ColumnVisibilities.Any(cv => cv.PageName == pageName && cv.RoleName == role && cv.ColumnName == column))
                        {
                            ColumnVisibilities.Add(new ColumnVisibility
                            {
                                PageName = pageName, // Renamed from 'page'
                                RoleName = role,
                                ColumnName = column,
                                IsVisible = true
                            });
                        }
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            foreach (var visibility in ColumnVisibilities)
            {
                var existing = await _context.ColumnVisibilities
                    .FirstOrDefaultAsync(cv => cv.PageName == visibility.PageName && 
                                              cv.RoleName == visibility.RoleName && 
                                              cv.ColumnName == visibility.ColumnName);
                if (existing != null)
                {
                    existing.IsVisible = visibility.IsVisible;
                    _context.Update(existing);
                }
                else
                {
                    _context.ColumnVisibilities.Add(visibility);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Column visibility settings updated.";
            return RedirectToPage();
        }

        private List<string> GetColumnsForPage(string pageName)
        {
            return pageName switch
            {
                "/Admin/LoginLog" => new List<string> { "UserId", "UserName", "LoginTime", "LogoutTime", "IsActive" },
                "/Admin/Users/Index" => new List<string> { "Id", "Email", "UserName" },
                _ => new List<string>()
            };
        }
    }
}