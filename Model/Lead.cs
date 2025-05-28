using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } // Non-nullable for [Required]

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } // Non-nullable for [Required]

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }

        [Display(Name = "Létrehozó")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Módosította")]
        public string? UpdatedBy { get; set; }

        public string? Status { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastContactDate { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string? Notes { get; set; }

        public int? PartnerId { get; set; } // Links to Partner
        [ForeignKey("PartnerId")]
        public Partner? Partner { get; set; } // Navigation property

        // Navigation property for history
        public ICollection<LeadHistory> LeadHistories { get; set; } = new List<LeadHistory>();
        public ICollection<CustomerCommunication>? Communications { get; set; }
    }
}