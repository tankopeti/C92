using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class VacationBalance
    {
        [Key]
        public int BalanceId { get; set; }

        [Required(ErrorMessage = "Employee ID is required.")]
        public int EmployeeId { get; set; }

        [Range(0, 99.99, ErrorMessage = "Available days must be between 0 and 99.99.")]
        public decimal? AvailableDays { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public Employees? Employee { get; set; }
    }
    public class VacationBalanceCreateDto
    {
        [Required(ErrorMessage = "Employee ID is required.")]
        public int EmployeeId { get; set; }

        [Range(0, 99.99, ErrorMessage = "Available days must be between 0 and 99.99.")]
        public decimal? AvailableDays { get; set; }
    }

    public class VacationBalanceUpdateDto
    {
        [Required(ErrorMessage = "Balance ID is required.")]
        public int BalanceId { get; set; }

        [Required(ErrorMessage = "Employee ID is required.")]
        public int EmployeeId { get; set; }

        [Range(0, 99.99, ErrorMessage = "Available days must be between 0 and 99.99.")]
        public decimal? AvailableDays { get; set; }
    }

}