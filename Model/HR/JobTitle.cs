using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class JobTitle
    {
        [Key]
        public int JobTitleId { get; set; }

        [StringLength(50)]
        public string? TitleName { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public ICollection<Employees>? Employees { get; set; }
    }

    public class JobTitleCreateDto
    {
        [StringLength(50, ErrorMessage = "Title name cannot exceed 50 characters.")]
        public string? TitleName { get; set; }
    }

    public class JobTitleUpdateDto
    {
        [Required(ErrorMessage = "Job title ID is required.")]
        public int JobTitleId { get; set; }

        [StringLength(50, ErrorMessage = "Title name cannot exceed 50 characters.")]
        public string? TitleName { get; set; }
    }

}