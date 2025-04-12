using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Cloud9_2.Models
{
    public class DocumentType
    {
        public int DocumentTypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string TypeName { get; set; }  // e.g., "Contract", "Invoice", "Report"

        public string? Description { get; set; }  // Optional description of the type

       // One DocumentType can have many Documents
        public ICollection<Document>? Documents { get; set; } = new List<Document>();
    }
}