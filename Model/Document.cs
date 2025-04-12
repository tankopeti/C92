using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Document
    {
        public int DocumentId { get; set; }

        [Required]
        public string FileName { get; set; }

        public string FilePath { get; set; }

        // Foreign key to DocumentType (one-to-many from DocumentType)
        public int? DocumentTypeId { get; set; }
        public DocumentType? DocumentType { get; set; }

        public DateTime? UploadDate { get; set; }

        public string? UploadedBy { get; set; }

        // Foreign key to Site (optional, one-to-many from Site)
        public int? SiteId { get; set; }  // Nullable to allow documents without a site
        public Site? Site { get; set; }

        // Foreign key to Partner (required, one-to-many from Partner)
        public int? PartnerId { get; set; }
        public Partner? Partner { get; set; }
    }
}