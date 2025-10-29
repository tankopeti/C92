using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
public class Vacation
    {
        [Key]
        public int VacationId { get; set; }

        [Required(ErrorMessage = "Employee ID is required.")]
        public int EmployeeId { get; set; }

        public int? AppUserId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(0, 99.99, ErrorMessage = "Duration must be between 0 and 99.99 days.")]
        public decimal? DurationDays { get; set; }

        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters.")]
        public string? Status { get; set; }

        [Range(0, 1, ErrorMessage = "IsApproved must be 0 (false) or 1 (true).")]
        public byte? IsApproved { get; set; }

        public string? Notes { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Employees? Employee { get; set; }
        public ApplicationUser? AppUser { get; set; }
    }
        public class VacationCreateDto
    {
        [Required(ErrorMessage = "Employee ID is required.")]
        public int EmployeeId { get; set; }

        public int? AppUserId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(0, 99.99, ErrorMessage = "Duration must be between 0 and 99.99 days.")]
        public decimal? DurationDays { get; set; }

        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters.")]
        public string? Status { get; set; }

        [Range(0, 1, ErrorMessage = "IsApproved must be 0 (false) or 1 (true).")]
        public byte? IsApproved { get; set; }

        public string? Notes { get; set; }
    }

    public class VacationUpdateDto
    {
        [Required(ErrorMessage = "Vacation ID is required.")]
        public int VacationId { get; set; }

        [Required(ErrorMessage = "Employee ID is required.")]
        public int EmployeeId { get; set; }

        public int? AppUserId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(0, 99.99, ErrorMessage = "Duration must be between 0 and 99.99 days.")]
        public decimal? DurationDays { get; set; }

        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters.")]
        public string? Status { get; set; }

        [Range(0, 1, ErrorMessage = "IsApproved must be 0 (false) or 1 (true).")]
        public byte? IsApproved { get; set; }

        public string? Notes { get; set; }
    }

}