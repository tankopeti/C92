using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Contact
    {
        [Key]
        public int ContactId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        [EmailAddress(ErrorMessage = "Érvénytelen email cím")]
        public string? Email { get; set; }
        [Phone(ErrorMessage = "Érvénytelen telefonszám")]
        public string? PhoneNumber { get; set; }
        [Phone(ErrorMessage = "Érvénytelen második telefonszám")]
        public string? PhoneNumber2 { get; set; }
        public string? JobTitle { get; set; }
        public string? Comment { get; set; }
        public string? Comment2 { get; set; }
        public bool? IsPrimary { get; set; } = false;
        public int? StatusId { get; set; }
        public Status? Status { get; set; }
        public int? PartnerId { get; set; }
        public Partner Partner { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public ICollection<CustomerCommunication>? CustomerCommunications { get; set; } = new List<CustomerCommunication>();
        public List<Document>? Documents { get; set; } = new List<Document>();
        public ICollection<Order>? Orders { get; set; }
    }

    public class ContactDto
    {
        [Key]
        public int ContactId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PhoneNumber2 { get; set; }
        public string? JobTitle { get; set; }
        public string? Comment { get; set; }
        public string? Comment2 { get; set; }
        public bool? IsPrimary { get; set; }
        public int? StatusId { get; set; }
        public Status? Status { get; set; }
        public int? PartnerId { get; set; }
        public Partner? Partner { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class CreateContactDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        [EmailAddress(ErrorMessage = "Érvénytelen email cím")]
        public string? Email { get; set; }
        [Phone(ErrorMessage = "Érvénytelen telefonszám")]
        public string? PhoneNumber { get; set; }
        [Phone(ErrorMessage = "Érvénytelen második telefonszám")]
        public string? PhoneNumber2 { get; set; }
        public string? JobTitle { get; set; }
        public string? Comment { get; set; }
        public string? Comment2 { get; set; }
        public bool? IsPrimary { get; set; } = false;
        public int? StatusId { get; set; }
        public int? PartnerId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }

    public class UpdateContactDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
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
        public int? StatusId { get; set; }
        public int PartnerId { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}