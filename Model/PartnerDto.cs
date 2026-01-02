using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Cloud9_2.Models
{
    public class PartnerDto
    {
         [Key]
        public int PartnerId { get; set; }

        [Required(ErrorMessage = "A név megadása kötelező")]
        [StringLength(100, ErrorMessage = "A név maximum 100 karakter hosszú lehet")]
        public string Name { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Érvénytelen email cím")]
        [StringLength(255, ErrorMessage = "Az email maximum 255 karakter hosszú lehet")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Érvénytelen telefonszám")]
        [StringLength(20, ErrorMessage = "A telefonszám maximum 20 karakter hosszú lehet")]
        public string? PhoneNumber { get; set; }

        [Phone(ErrorMessage = "Érvénytelen másodlagos telefonszám")]
        [StringLength(20, ErrorMessage = "A másodlagos telefonszám maximum 20 karakter hosszú lehet")]
        public string? AlternatePhone { get; set; }

        [Url(ErrorMessage = "Érvénytelen weboldal URL")]
        [StringLength(255, ErrorMessage = "A weboldal maximum 255 karakter hosszú lehet")]
        public string? Website { get; set; }

        [StringLength(100, ErrorMessage = "A cég neve maximum 100 karakter hosszú lehet")]
        public string? CompanyName { get; set; }

        [StringLength(50, ErrorMessage = "Az adószám maximum 50 karakter hosszú lehet")]
        public string? TaxId { get; set; }

        [StringLength(50, ErrorMessage = "A nemzetközi adószám maximum 50 karakter hosszú lehet")]
        public string? IntTaxId { get; set; }

        [StringLength(50, ErrorMessage = "Az iparág maximum 50 karakter hosszú lehet")]
        public string? Industry { get; set; }

        [StringLength(100, ErrorMessage = "Az utca és házszám maximum 100 karakter hosszú lehet")]
        public string? AddressLine1 { get; set; }

        [StringLength(100, ErrorMessage = "Az utca és házszám 2 maximum 100 karakter hosszú lehet")]
        public string? AddressLine2 { get; set; }

        [StringLength(50, ErrorMessage = "A város maximum 50 karakter hosszú lehet")]
        public string? City { get; set; }

        [StringLength(50, ErrorMessage = "A megye maximum 50 karakter hosszú lehet")]
        public string? State { get; set; }

        [StringLength(20, ErrorMessage = "Az irányítószám maximum 20 karakter hosszú lehet")]
        public string? PostalCode { get; set; }

        [StringLength(50, ErrorMessage = "Az ország maximum 50 karakter hosszú lehet")]
        public string? Country { get; set; }

        [Display(Name = "Státusz")]
        public int? StatusId { get; set; } // Foreign key, nullable to match original string?

        // [ForeignKey("StatusId")]
        // public Status? Status { get; set; } // Navigation property

        public StatusDto? Status { get; set; }

        public DateTime? LastContacted { get; set; }

        [StringLength(1000, ErrorMessage = "A jegyzet maximum 1000 karakter hosszú lehet")]
        public string? Notes { get; set; }

        [StringLength(50, ErrorMessage = "Az értékesítő maximum 50 karakter hosszú lehet")]
        public string? AssignedTo { get; set; }

        [StringLength(100, ErrorMessage = "A számlázási kapcsolattartó maximum 100 karakter hosszú lehet")]
        public string? BillingContactName { get; set; }

        [EmailAddress(ErrorMessage = "Érvénytelen számlázási email cím")]
        [StringLength(255, ErrorMessage = "A számlázási email maximum 255 karakter hosszú lehet")]
        public string? BillingEmail { get; set; }

        [StringLength(50, ErrorMessage = "A fizetési feltételek maximum 50 karakter hosszú lehet")]
        public string? PaymentTerms { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A kredit limit nem lehet negatív")]
        public decimal? CreditLimit { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Az alap valuta pontosan 3 karakter hosszú kell legyen (pl. USD)")]
        public string? PreferredCurrency { get; set; }

        public bool? IsTaxExempt { get; set; }

        public int? PartnerGroupId { get; set; }

        public bool IsActive { get; set; } = true;

        public List<SiteDto>? Sites { get; set; } = new List<SiteDto>();
        public List<ContactDto>? Contacts { get; set; } = new List<ContactDto>();
        public List<DocumentDto>? Documents { get; set; } = new List<DocumentDto>();
    }
    
}