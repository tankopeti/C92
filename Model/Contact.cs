using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Contact
    {
        public int ContactId { get; set; }
        public string FirstName { get; set; } 
        public string LastName { get; set; } 
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? JobTitle { get; set; }
        public string? Comment { get; set; }
        public bool IsPrimary { get; set; } = false;

        public ICollection<CustomerCommunication> CustomerCommunications { get; set; } = new List<CustomerCommunication>();
        public List<Document> Documents { get; set; } = new List<Document>();

        // Foreign key to Partner
        [Required(ErrorMessage = "Partner ID is required")]
        public int PartnerId { get; set; }
        public Partner Partner { get; set; }
    }

    public class ContactDto
    {
        public int ContactId { get; set; }
        public string FirstName { get; set; } // Removed [Required]
        public string LastName { get; set; }  // Removed [Required]
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? JobTitle { get; set; }
        public string? Comment { get; set; }
        public bool IsPrimary { get; set; }
    }
}