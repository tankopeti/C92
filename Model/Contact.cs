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
        public bool IsPrimary { get; set; }

        public ICollection<CustomerCommunication> CustomerCommunications { get; set; } = new List<CustomerCommunication>();

        // Foreign key to Partner
        public int PartnerId { get; set; }
        public Partner Partner { get; set; }
    }
}