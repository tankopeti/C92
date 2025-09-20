using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Contact
    {
        [Key]
        public int ContactId { get; set; }

        [Required(ErrorMessage = "A vezetéknév kötelező")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "A keresztnév kötelező")]
        public string LastName { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Érvénytelen email cím")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Érvénytelen telefonszám")]
        public string? PhoneNumber { get; set; }

        [Phone(ErrorMessage = "Érvénytelen második telefonszám")]
        public string? PhoneNumber2 { get; set; }

        public string? JobTitle { get; set; }
        public string? Comment { get; set; }
        public string? Comment2 { get; set; }
        public bool IsPrimary { get; set; } = false;

        [Display(Name = "Státusz")]
        public int? StatusId { get; set; }

        public Status? Status { get; set; }

        [Required(ErrorMessage = "Partner ID is required")]
        public int PartnerId { get; set; }
        public Partner Partner { get; set; } = null!;

        public ICollection<CustomerCommunication> CustomerCommunications { get; set; } = new List<CustomerCommunication>();
        public List<Document> Documents { get; set; } = new List<Document>();
    }

    public class ContactDto
    {
        [Key]
        public int ContactId { get; set; }
        public string FirstName { get; set; } 
        public string LastName { get; set; }  
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PhoneNumber2 { get; set; }
        public string? JobTitle { get; set; }
        public string? Comment { get; set; }
        public string? Comment2 { get; set; }
        public bool IsPrimary { get; set; }
        public int? StatusId { get; set; }
        public Status? Status { get; set; }
    }
}