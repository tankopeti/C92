using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;

namespace Cloud9_2.Pages.Admin
{
    [Authorize(Roles = "SuperAdmin, Admin")]
    public class ManageRolesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ManageRolesModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Role { get; set; }

        public List<string> Roles { get; set; }
        public string Message { get; set; }
        public IList<UserWithRolesViewModel> UsersWithRoles { get; set; }

        public async Task OnGetAsync()
        {
            Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            // Materialize the users list
            var userList = await _userManager.Users.ToListAsync();

            // Fetch roles for each user
            UsersWithRoles = new List<UserWithRolesViewModel>();
            foreach (var u in userList)
            {
                var roles = await _userManager.GetRolesAsync(u);
                UsersWithRoles.Add(new UserWithRolesViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Roles = roles.ToList()
                });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Role))
            {
                Message = "Please provide both an email and a role.";
                Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

                // Materialize the users list
                var userList = await _userManager.Users.ToListAsync();
                UsersWithRoles = new List<UserWithRolesViewModel>();
                foreach (var u in userList)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    UsersWithRoles.Add(new UserWithRolesViewModel
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        Roles = roles.ToList()
                    });
                }
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                Message = "User not found.";
            }
            else if (!await _roleManager.RoleExistsAsync(Role))
            {
                Message = "Role does not exist.";
            }
            else
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Contains(Role))
                {
                    Message = $"{user.UserName} already has the role {Role}.";
                }
                else
                {
                    var result = await _userManager.AddToRoleAsync(user, Role);
                    if (result.Succeeded)
                    {
                        Message = $"Successfully assigned role {Role} to {user.UserName}.";
                    }
                    else
                    {
                        Message = "Error assigning role: " + string.Join(", ", result.Errors.Select(e => e.Description));
                    }
                }
            }

            Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var usersPost = await _userManager.Users.ToListAsync();
            UsersWithRoles = new List<UserWithRolesViewModel>();
            foreach (var u in usersPost)
            {
                var roles = await _userManager.GetRolesAsync(u);
                UsersWithRoles.Add(new UserWithRolesViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Roles = roles.ToList()
                });
            }

            return Page();
        }
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveRoleAsync([FromBody] RemoveRoleDto dto)
        {
            try
            {
                Console.WriteLine($"RemoveRole called with UserId: {dto?.UserId}, Role: {dto?.Role}");

                if (string.IsNullOrEmpty(dto?.UserId) || string.IsNullOrEmpty(dto?.Role))
                {
                    return new JsonResult(new { success = false, message = "User ID and role are required." });
                }

                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found." });
                }

                var normalizedRole = dto.Role.Trim();
                if (!await _roleManager.RoleExistsAsync(normalizedRole))
                {
                    return new JsonResult(new { success = false, message = "Role does not exist." });
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(normalizedRole))
                {
                    return new JsonResult(new { success = false, message = $"{user.UserName} does not have the role {normalizedRole}." });
                }

                var result = await _userManager.RemoveFromRoleAsync(user, normalizedRole);
                if (result.Succeeded)
                {
                    return new JsonResult(new { success = true, message = $"Role {normalizedRole} removed from {user.UserName}." });
                }

                return new JsonResult(new { success = false, message = "Error removing role: " + string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveRole: {ex.Message}\n{ex.StackTrace}");
                return new JsonResult(new { success = false, message = $"Server error: {ex.Message}" });
            }
        }

        
    }

    public class UserWithRolesViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
    }

    public class RemoveRoleDto
    {
        public string UserId { get; set; }
        public string Role { get; set; }
    }
}