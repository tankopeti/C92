using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Cloud9_2.Models
{
    public class Partner
    {
        // Core Identification
        [Key]
        public int PartnerId { get; set; }              // Unique identifier (primary key)

        // Basic Contact Information
        [Display(Name = "Név")]
        public string Name { get; set; }                // Individual or company name

        [Display(Name = "Email")]
        public string? Email { get; set; }               // Primary email for communication
        
        [Display(Name = "Telefon1")]
        public string? PhoneNumber { get; set; }         // Primary phone number
        
        [Display(Name = "Telefon2")]
        public string? AlternatePhone { get; set; }      // Secondary phone (optional)
        
        [Display(Name = "Weboldal")]
        public string? Website { get; set; }             // Partner's website (optional)

        // Business Details
        
        [Display(Name = "Teljes név")]
        public string? CompanyName { get; set; }         // Legal business name (if applicable)
        
        [Display(Name = "Adószám")]
        public string? TaxId { get; set; }               // Tax identification number (e.g., EIN, VAT)
        
        [Display(Name = "Nemzetközi Adószám")]
        public string? IntTaxId { get; set; }               // Tax identification number (e.g., EIN, VAT)
        
        [Display(Name = "Iparág")]
        public string? Industry { get; set; }            // e.g., "Technology", "Retail"

        // Physical Address (for invoicing and CRM)
        
        [Display(Name = "Utca házszám")]
        public string? AddressLine1 { get; set; }        // Street address
        
        [Display(Name = "Utca házszám 2")]
        public string? AddressLine2 { get; set; }        // Additional address line (optional)
        
        [Display(Name = "Város")]
        public string? City { get; set; }                // City
        
        [Display(Name = "Megye")]
        public string? State { get; set; }               // State/Province
        
        [Display(Name = "Irányítószám")]
        public string? PostalCode { get; set; }          // ZIP or postal code
        
        [Display(Name = "Ország")]
        public string? Country { get; set; }             // Country

        // CRM-Specific Properties

        [Display(Name = "Utolsó kapcsolat dátuma")]
        public DateTime? LastContacted { get; set; }     // Date of last interaction

        [Display(Name = "Jegyzet")]
        public string? Notes { get; set; }               // Free-text field for CRM notes

        [Display(Name = "Értékesítő")]
        public string? AssignedTo { get; set; }          // User or team responsible (e.g., username or ID)

        // Invoicing-Specific Properties

        [Display(Name = "Számlázási kapcsolattartó")]
        public string? BillingContactName { get; set; }  // Contact person for invoices

        [Display(Name = "Számlázási email")]
        public string? BillingEmail { get; set; }        // Email for sending invoices

        [Display(Name = "Fizetési feltételek")]
        public string? PaymentTerms { get; set; }        // e.g., "Net 30", "Due on Receipt"

        [Display(Name = "Kredit limit")]
        public decimal? CreditLimit { get; set; }        // Maximum credit allowed

        [Display(Name = "Alap Valuta")]
        public string? PreferredCurrency { get; set; }   // e.g., "USD", "EUR"

        [Display(Name = "Adómentes")]
        public bool? IsTaxExempt { get; set; }           // Flag for tax exemption status

        [Display(Name = "Partnercsoport")]
        public int? PartnerGroupId { get; set; } // Foreign key (nullable for optional group)

        [ForeignKey("PartnerGroupId")]
        public PartnerGroup? PartnerGroup { get; set; } // Navigation property
        [Display(Name = "Státusz")]
        public int? StatusId { get; set; }
        public Status? Status { get; set; }
        

        // Audit Fields

        [Display(Name = "Létrehozás dátuma")]
        public DateTime? CreatedDate { get; set; }       // When the partner was added

        [Display(Name = "Utolsó frissítés dátuma")]
        public DateTime? UpdatedDate { get; set; }      // Last update timestamp (nullable)

        [Display(Name = "Létrehozó")]
        public string? CreatedBy { get; set; }           // User who created the record

        [Display(Name = "Módosította")]
        public string? UpdatedBy { get; set; }           // User who last updated (optional)
        public List<Lead>? Leads { get; set; } // Collection for one-to-many

        // Navigation properties for one-to-many relationships
        public List<Site>? Sites { get; set; }         // Multiple sites per partner
        public List<Contact>? Contacts { get; set; }   // Multiple contacts per partner
        public List<Document>? Documents { get; set; } // Multiple documents per partner
        public List<PartnerType>? PartnerTypes { get; set; } // Multiple Types per partner e.g., "Client", "Vendor", "Both"
        public List<LeadSource>? LeadSources { get; set; } // Multiple Lead sources e.g., "Website", "Referral"
        public List<Quote>? Quotes { get; set; } 
        public ICollection<Order>? Orders { get; set; }
        public ICollection<CustomerCommunication>? Communications { get; set; }
 
        // Constructor to initialize collections
        public Partner()
        {
            CreatedDate = DateTime.Now;
            IsTaxExempt = false;
            Sites = new List<Site>();
            Contacts = new List<Contact>();
            Documents = new List<Document>();
            Leads = new List<Lead>();
            Quotes = new List<Quote>();
        }

    }
}