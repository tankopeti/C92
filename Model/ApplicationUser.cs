using Microsoft.AspNetCore.Identity;

namespace Cloud9_2.Models
{

    public class ApplicationUser : IdentityUser
    {
        public bool? MustChangePassword { get; set; } = false;
        public bool? Disabled { get; set; } = false;

        public ICollection<CustomerCommunication> CustomerCommunications { get; set; } = new List<CustomerCommunication>();
    }
}