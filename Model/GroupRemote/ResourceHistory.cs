using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class ResourceHistory
    {
        [Key]
        public int Id { get; set; }

        public int ResourceId { get; set; }
        public Resource Resource { get; set; }

        public string? ModifiedById { get; set; }
        public ApplicationUser? ModifiedBy { get; set; }

        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; } = DateTime.UtcNow;

        [StringLength(500, ErrorMessage = "Change description cannot exceed 500 characters")]
        public string? ChangeDescription { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Service price must be between 0 and 999,999.99")]
        public decimal? ServicePrice { get; set; }
    }
    public class ResourceHistoryDto
    {
        public int Id { get; set; }
        public int ResourceId { get; set; }
        public string? ModifiedById { get; set; }
        public string? ModifiedByName { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ChangeDescription { get; set; }
        public decimal? ServicePrice { get; set; }
    }

}