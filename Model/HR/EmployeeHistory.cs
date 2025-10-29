using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class EmployeeHistory
    {
        [Key]
        public int HistoryId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        public int? AppUserId { get; set; }

        public DateTime? ChangeTimestamp { get; set; } = DateTime.UtcNow;

        public string? WhatModified { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Employees? Employee { get; set; }
        public ApplicationUser? AppUser { get; set; }
    }
    public class EmployeeHistoryCreateDto
    {
        [Required(ErrorMessage = "Employee ID is required.")]
        public int EmployeeId { get; set; }

        public int? AppUserId { get; set; }

        public string? WhatModified { get; set; }
    }

    public class EmployeeHistoryUpdateDto
    {
        [Required(ErrorMessage = "History ID is required.")]
        public int HistoryId { get; set; }

        [Required(ErrorMessage = "Employee ID is required.")]
        public int EmployeeId { get; set; }

        public int? AppUserId { get; set; }

        public string? WhatModified { get; set; }
    }
}