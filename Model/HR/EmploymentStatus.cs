using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class EmploymentStatus
    {
        [Key]
        public int StatusId { get; set; }

        [StringLength(50)]
        public string? StatusName { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public ICollection<Employees>? Employees { get; set; }
    }

    public class EmploymentStatusCreateDto
    {
        [StringLength(50, ErrorMessage = "Status name cannot exceed 50 characters.")]
        public string? StatusName { get; set; }
    }

    public class EmploymentStatusUpdateDto
    {
        [Required(ErrorMessage = "Status ID is required.")]
        public int StatusId { get; set; }

        [StringLength(50, ErrorMessage = "Status name cannot exceed 50 characters.")]
        public string? StatusName { get; set; }
    }

}