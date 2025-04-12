using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Cloud9_2.Models;

namespace Cloud9_2.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class EditUserModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public EditUserModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string Id { get; set; }

            [Required]
            public string UserName { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            public string PhoneNumber { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Access Failed Count must be a non-negative number.")]
            public int AccessFailedCount { get; set; }

            public bool Disabled { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("User ID is required.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            Input = new InputModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AccessFailedCount = user.AccessFailedCount,
                Disabled = user.Disabled.GetValueOrDefault(false)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            user.UserName = Input.UserName;
            user.Email = Input.Email;
            user.PhoneNumber = Input.PhoneNumber;
            user.AccessFailedCount = Input.AccessFailedCount;
            user.Disabled = Input.Disabled;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
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
    }
}