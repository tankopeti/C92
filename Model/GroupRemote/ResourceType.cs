using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class ResourceType
    {
        [Key]
        public int ResourceTypeId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

        public bool? IsActive { get; set; } = true; // Matches database BIT NULL

        public ICollection<Resource>? Resources { get; set; } = new List<Resource>();
    }
}