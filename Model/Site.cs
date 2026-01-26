using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Cloud9_2.Models
{
    public class Site
    {
        public int SiteId { get; set; }
        [Required(ErrorMessage = "Telephely neve kötelező")]
        [Display(Name = "Telephely neve")]
        public string? SiteName { get; set; }
        [Display(Name = "Cím 1")]
        public string? AddressLine1 { get; set; }
        [Display(Name = "Cím 2")]
        public string? AddressLine2 { get; set; }
        [Display(Name = "Város")]
        public string? City { get; set; }
        [Display(Name = "Állam/Megye")]
        public string? State { get; set; }
        [Display(Name = "Irányítószám")]
        public string? PostalCode { get; set; }
        [Display(Name = "Ország")]
        public string? Country { get; set; }
        [Display(Name = "Elsődleges")]
        public bool IsPrimary { get; set; } = false;

        [Display(Name = "Kapcsolattartó 1")]
        public string? ContactPerson1 { get; set; }

        [Display(Name = "Kapcsolattartó 2")]
        public string? ContactPerson2 { get; set; }

        [Display(Name = "Kapcsolattartó 3")]
        public string? ContactPerson3 { get; set; }


        [Display(Name = "Vezetékes telefonszám (1)")]
        public string? Phone1 { get; set; }

        [Display(Name = "Vezetékes telefonszám (2)")]
        public string? Phone2 { get; set; }

        [Display(Name = "Vezetékes telefonszám (3)")]
        public string? Phone3 { get; set; }



        [Display(Name = "Mobil (1)")]
        public string? MobilePhone1 { get; set; }

        [Display(Name = "Mobil (2)")]
        public string? MobilePhone2 { get; set; }

        [Display(Name = "Mobil (3)")]
        public string? MobilePhone3 { get; set; }


        [Display(Name = "Üzenető alkalmazás 1")]
        public string? messagingApp1 { get; set; }

        [Display(Name = "Üzenető alkalmazás 2")]
        public string? messagingApp2 { get; set; }

        [Display(Name = "Üzenető alkalmazás 3")]
        public string? messagingApp3 { get; set; }


        [Display(Name = "e-Mail (1)")]
        public string? eMail1 { get; set; }

        [Display(Name = "e-Mail (2)")]
        public string? eMail2 { get; set; }


        [Display(Name = "Megjegyzés 1")]
        public string? Comment1 { get; set; }
        [Display(Name = "Megjegyzés 2")]
        public string? Comment2 { get; set; }
        // Audit fields
        [Display(Name = "Létrehozva")]
        public DateTime? CreatedDate { get; set; }
        public string? CreatedById { get; set; }
        public ApplicationUser? CreatedBy { get; set; }
        [Display(Name = "Utoljára módosítva")]
        public DateTime? LastModifiedDate { get; set; }
        public string? LastModifiedById { get; set; }
        public ApplicationUser? LastModifiedBy { get; set; }

        // Foreign key to Partner
        [Required(ErrorMessage = "Partner azonosító kötelező")]
        [Display(Name = "Partner")]
        public int PartnerId { get; set; }

        [ForeignKey("PartnerId")]
        public Partner Partner { get; set; }

        // Navigation properties
        public ICollection<Quote>? Quotes { get; set; } = new List<Quote>();
        public ICollection<Document>? Documents { get; set; } = new List<Document>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Resource>? Resources { get; set; } = new List<Resource>();
        public ICollection<CustomerCommunication> CustomerCommunications { get; set; } = new List<CustomerCommunication>();

        // public ICollection<Employee>? Employee { get; set; } = new List<Employee>();

        [Display(Name = "Státusz")]
        public int? StatusId { get; set; }
        public Status? Status { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SiteDto
    {
        [Key]
        public int SiteId { get; set; }
        [Display(Name = "Telephely neve")]
        public string? SiteName { get; set; }
        [Display(Name = "Cím 1")]
        public string? AddressLine1 { get; set; }
        [Display(Name = "Cím 2")]
        public string? AddressLine2 { get; set; }
        [Display(Name = "Város")]
        public string? City { get; set; }
        [Display(Name = "Állam/Megye")]
        public string? State { get; set; }
        [Display(Name = "Irányítószám")]
        public string? PostalCode { get; set; }
        [Display(Name = "Ország")]
        public string? Country { get; set; }
        [Display(Name = "Elsődleges")]
        public bool IsPrimary { get; set; }

        [Display(Name = "Vezetékes telefonszám (1)")]
        public string? Phone1 { get; set; }

        [Display(Name = "Vezetékes telefonszám (2)")]
        public string? Phone2 { get; set; }

        [Display(Name = "Vezetékes telefonszám (3)")]
        public string? Phone3 { get; set; }


        [Display(Name = "Mobil (1)")]
        public string? MobilePhone1 { get; set; }

        [Display(Name = "Mobil (2)")]
        public string? MobilePhone2 { get; set; }

        [Display(Name = "Mobil (3)")]
        public string? MobilePhone3 { get; set; }


        [Display(Name = "Üzenető alkalmazás 1")]
        public string? messagingApp1 { get; set; }

        [Display(Name = "Üzenető alkalmazás 2")]
        public string? messagingApp2 { get; set; }

        [Display(Name = "Üzenető alkalmazás 3")]
        public string? messagingApp3 { get; set; }


        [Display(Name = "e-Mail (1)")]
        public string? eMail1 { get; set; }

        [Display(Name = "e-Mail (2)")]
        public string? eMail2 { get; set; }


        [Display(Name = "Kapcsolattartó 1")]
        public string? ContactPerson1 { get; set; }
        [Display(Name = "Kapcsolattartó 2")]
        public string? ContactPerson2 { get; set; }
        [Display(Name = "Kapcsolattartó 3")]
        public string? ContactPerson3 { get; set; }
        [Display(Name = "Megjegyzés 1")]
        public string? Comment1 { get; set; }
        [Display(Name = "Megjegyzés 2")]
        public string? Comment2 { get; set; }

        [Display(Name = "Státusz")]
        public int? StatusId { get; set; }

        public int PartnerId { get; set; }


        public bool IsActive { get; set; } = true;
    }
}