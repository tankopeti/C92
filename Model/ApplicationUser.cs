using Microsoft.AspNetCore.Identity;

namespace Cloud9_2.Models
{

    public class ApplicationUser : IdentityUser
    {
        public bool? MustChangePassword { get; set; } = false;
        public bool? Disabled { get; set; } = false;
    }
}