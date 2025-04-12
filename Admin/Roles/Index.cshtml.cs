// Pages/Admin/Roles.cshtml.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Cloud9_2.Pages.Admin.Roles
{
    [Authorize(Roles = "SuperAdmin, Admin")]
    public class IndexModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public IList<IdentityRole> Roles { get; set; }

        public async Task OnGetAsync()
        {
            Roles = await _roleManager.Roles.ToListAsync();
        }

        public async Task<IActionResult> OnPostAddRoleAsync([FromBody] RoleDto roleDto)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(roleDto?.RoleName))
            {
                return new BadRequestResult();
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleDto.RoleName));
            if (!result.Succeeded)
            {
                return new BadRequestResult();
            }

            var role = await _roleManager.FindByNameAsync(roleDto.RoleName);
            return new JsonResult(new
            {
                id = role.Id,
                name = role.Name,
                normalizedName = role.NormalizedName ?? string.Empty
            });
        }

        public async Task<IActionResult> OnPostUpdateRoleAsync([FromBody] RoleUpdateDto data)
{
    if (string.IsNullOrEmpty(data?.Id) || string.IsNullOrEmpty(data?.Name))
    {
        return new BadRequestResult();
    }

    var role = await _roleManager.FindByIdAsync(data.Id);
    if (role == null)
    {
        return new NotFoundResult();
    }

    role.Name = data.Name;
    role.NormalizedName = _roleManager.NormalizeKey(data.Name);
    
    var result = await _roleManager.UpdateAsync(role);
    
    return result.Succeeded ? new OkResult() : new BadRequestResult();
}




        public async Task<IActionResult> OnDeleteDeleteRoleAsync(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return new NotFoundResult();
            }

            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded ? new OkResult() : new BadRequestResult();
        }

        public class RoleDto
        {
            public string RoleName { get; set; }
        }
public class RoleUpdateDto
{
    public string Id { get; set; }
    public string Name { get; set; }
}
    }
}
