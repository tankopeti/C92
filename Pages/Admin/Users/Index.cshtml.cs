using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloud9_2.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Cloud9_2.Pages.Admin.Users
{
    [Authorize(Roles = "SuperAdmin, Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IList<UserViewModel> Users { get; set; }

        public async Task OnGetAsync()
        {
            Users = await _userManager.Users
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    LockoutEnabled = u.LockoutEnabled,
                    EmailConfirmed = u.EmailConfirmed,
                    PhoneNumberConfirmed = u.PhoneNumberConfirmed,
                    AccessFailedCount = u.AccessFailedCount,
                    TwoFactorEnabled = u.TwoFactorEnabled,
                    Disabled = u.Disabled.GetValueOrDefault(false)
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAddUserAsync([FromBody] UserDtoWrapper wrapper)
        {
            try
            {
                Console.WriteLine($"Received request: {JsonSerializer.Serialize(wrapper)}");

                if (wrapper?.UserDto == null)
                {
                    return BadRequest(new { error = "Invalid request data: UserDto is null" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors.Select(e => new
                        {
                            Field = x.Key.Replace("UserDto.", ""),
                            Message = e.ErrorMessage
                        }));
                    return BadRequest(new { Errors = errors });
                }

                var user = new ApplicationUser
                {
                    UserName = wrapper.UserDto.UserName,
                    Email = wrapper.UserDto.Email,
                    PhoneNumber = wrapper.UserDto.PhoneNumber,
                    MustChangePassword = false
                };

                var result = await _userManager.CreateAsync(user, wrapper.UserDto.Password);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => new
                    {
                        Field = e.Code.Contains("Email") ? "Email" :
                                e.Code.Contains("Password") ? "Password" :
                                e.Code.Contains("UserName") ? "UserName" : "",
                        Message = e.Description
                    });
                    return BadRequest(new { Errors = errors });
                }

                return new JsonResult(new
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    AccessFailedCount = user.AccessFailedCount,
                    Disabled = user.Disabled
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in OnPostAddUserAsync: {ex}");
                return BadRequest(new { error = $"Server error: {ex.Message}" });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteUserAsync([FromBody] DeleteUserDto dto)
        {
            if (string.IsNullOrEmpty(dto?.UserId))
            {
                return new JsonResult(new { success = false, message = "User ID is required." });
            }

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "User not found." });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return new JsonResult(new { success = true, message = $"User {user.UserName} deleted successfully." });
            }

            return new JsonResult(new { success = false, message = "Error deleting user: " + string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        public async Task<IActionResult> OnPostForcePasswordChangeAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            user.MustChangePassword = true;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new { Field = "", Message = e.Description });
                return BadRequest(new { Errors = errors });
            }

            return new JsonResult(new { success = true, message = $"User {user.UserName} must now change their password." });
        }

        public async Task<IActionResult> OnPostEditUserAsync([FromBody] EditUserDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Id))
            {
                return BadRequest(new { error = "Invalid user data" });
            }

            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.AccessFailedCount = dto.AccessFailedCount;
            user.Disabled = dto.Disabled;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new { Field = e.Code, Message = e.Description });
                return BadRequest(new { Errors = errors });
            }

            return new JsonResult(new
            {
                success = true,
                user = new
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    AccessFailedCount = user.AccessFailedCount,
                    Disabled = user.Disabled
                }
            });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostResetPasswordAsync([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto?.UserId))
                {
                    return new JsonResult(new { success = false, message = "User ID is required." });
                }

                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found." });
                }

                // Generate new password: username + current date + "C92" (e.g., "john.doe20250403C92")
                string today = DateTime.UtcNow.ToString("yyyyMMdd");
                string newPassword = $"{user.UserName}{today}C92";

                // Reset password
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!resetResult.Succeeded)
                {
                    return new JsonResult(new { success = false, message = "Error resetting password: " + string.Join(", ", resetResult.Errors.Select(e => e.Description)) });
                }

                // Require password change on next login
                user.MustChangePassword = true;
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    return new JsonResult(new { success = false, message = "Error setting password change requirement: " + string.Join(", ", updateResult.Errors.Select(e => e.Description)) });
                }

                return new JsonResult(new { success = true, message = $"Password reset to {newPassword} for {user.UserName}. User must change it on next login." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in OnPostResetPasswordAsync: {ex}");
                return new JsonResult(new { success = false, message = $"Server error: {ex.Message}" });
            }
        }
    }

    public class EditUserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int AccessFailedCount { get; set; }
        public bool Disabled { get; set; }
    }

    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool LockoutEnabled { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public bool MustChangePassword { get; set; }
        public bool Disabled { get; set; }
    }

    public class UserDto
    {
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; }
    }

    public class UserDtoWrapper
    {
        public UserDto UserDto { get; set; }
    }

    public class DeleteUserDto
    {
        public string UserId { get; set; }
    }

    public class ResetPasswordDto
    {
        public string UserId { get; set; }
    }
}