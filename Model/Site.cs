using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Site
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public bool IsPrimary { get; set; } = false;

        // Audit fields
        public DateTime? CreatedDate { get; set; }
        public string? CreatedById { get; set; }
        public ApplicationUser? CreatedBy { get; set; }
        
        public DateTime? LastModifiedDate { get; set; }
        public string? LastModifiedById { get; set; }
        public ApplicationUser? LastModifiedBy { get; set; }

        // Foreign key to Partner
        public int? PartnerId { get; set; }
        public Partner? Partner { get; set; }

        // Navigation property for Documents (one-to-many)
        public ICollection<Document>? Documents { get; set; } = new List<Document>();
    }
}