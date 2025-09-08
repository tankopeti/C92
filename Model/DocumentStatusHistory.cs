using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class DocumentStatusHistory
    {
        public int Id { get; set; }
        public int DocumentId { get; set; } // Foreign key to Document
        [Required]
        public DocumentStatusEnum OldStatus { get; set; } // Previous status
        [Required]
        public DocumentStatusEnum NewStatus { get; set; } // New status
        [Required]
        public DateTime ChangeDate { get; set; } // When the change occurred
        [Required]
        public string ChangedBy { get; set; } // Who made the change
        public Document Document { get; set; } // Navigation property
    }
}