using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Cloud9_2.Models
{
    public class TaskTypePM
    {
        public int TaskTypePMId { get; set; }

        [Required]
        [StringLength(50)]
        public string TaskTypePMName { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Icon { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<TaskPM>? Tasks { get; set; }
    }
}