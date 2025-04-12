// File: YourProject/Models/ContactInputModel.cs (or ViewModels/)

using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class ContactInputModel
    {
        // Required to link back to the partner
        public int PartnerId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [EmailAddress]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [Display(Name = "Comment")]
        public string? Comment { get; set; }

        [Display(Name = "Is Primary Contact?")]
        public bool IsPrimary { get; set; }
    }
}