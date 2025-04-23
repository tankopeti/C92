using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class LeadHistory
    {
        [Key]
        public int LeadHistoryId { get; set; }
        public int LeadId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public int? PartnerId { get; set; }
        public string? CreatedById { get; set; } // Added
        public DateTime CreatedDate { get; set; } // Added
        public string? LastModifiedById { get; set; } // Added
        public string? ChangedBy { get; set; } // Kept for compatibility
        public DateTime ChangeDate { get; set; } // Kept for compatibility
        public string? ChangeType { get; set; }

        public Lead Lead { get; set; }
    }
}