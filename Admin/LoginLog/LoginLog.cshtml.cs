using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Cloud9_2.Models;
using Cloud9_2.Data;

namespace Cloud9_2.Pages.Admin
{
    [Authorize(Roles = "SuperAdmin, Admin")]
    public class LoginLogModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginLogModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<UserActivity> UserActivities { get; set; }
        public Dictionary<string, bool> ColumnVisibility { get; set; }

        public async Task OnGetAsync()
        {
            System.Console.WriteLine("OnGetAsync started.");

            // Ensure only the latest 500 records are kept
            await CleanupOldRecordsAsync();

            // Load records
            UserActivities = await _context.UserActivities
                .OrderByDescending(ua => ua.LoginTime)
                .Take(500)
                .ToListAsync();

            System.Console.WriteLine($"UserActivities Count: {UserActivities.Count}");
            if (UserActivities.Any())
            {
                System.Console.WriteLine($"Sample Record: UserId={UserActivities[0].UserId}, UserName={UserActivities[0].UserName}, LoginTime={UserActivities[0].LoginTime}");
            }
            else
            {
                System.Console.WriteLine("UserActivities is empty.");
            }

            // Column visibility setup
            var allColumns = new List<string> { "UserId", "UserName", "LoginTime", "LogoutTime", "IsActive" };
            var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            System.Console.WriteLine($"User Roles: {string.Join(", ", userRoles)}");

            var visibilitySettings = await _context.ColumnVisibilities
                .Where(cv => cv.PageName == "/Admin/LoginLog" && userRoles.Contains(cv.RoleName))
                .ToListAsync();

            ColumnVisibility = allColumns.ToDictionary(col => col, col => false);
            foreach (var setting in visibilitySettings)
            {
                if (allColumns.Contains(setting.ColumnName))
                {
                    ColumnVisibility[setting.ColumnName] = setting.IsVisible;
                }
            }

            System.Console.WriteLine("Column Visibility:");
            foreach (var kv in ColumnVisibility)
            {
                System.Console.WriteLine($"{kv.Key}: {kv.Value}");
            }
        }

        private async Task CleanupOldRecordsAsync()
        {
            int maxRecords = 500;
            int totalRecords = await _context.UserActivities.CountAsync();
            System.Console.WriteLine($"Records Before Cleanup: {totalRecords}");

            if (totalRecords > maxRecords)
            {
                var recordsToDelete = await _context.UserActivities
                    .OrderBy(ua => ua.LoginTime)
                    .Take(totalRecords - maxRecords)
                    .ToListAsync();

                _context.UserActivities.RemoveRange(recordsToDelete);
                await _context.SaveChangesAsync();
                System.Console.WriteLine($"Records After Cleanup: {await _context.UserActivities.CountAsync()}");
            }
            else
            {
                System.Console.WriteLine("No cleanup needed.");
            }
        }
    }
}