using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Salary
    {
        [Key]
        public int SalaryId { get; set; }

        public int? EmployeeId { get; set; }

        [Range(0, 99999999.99)]
        public decimal? SalaryAmount { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public Employees? Employee { get; set; }
    }
        public class SalaryCreateDto
    {
        public int? EmployeeId { get; set; }

        [Range(0, 99999999.99, ErrorMessage = "Salary amount must be between 0 and 99,999,999.99.")]
        public decimal? SalaryAmount { get; set; }

        public DateTime? EffectiveDate { get; set; }
    }

    public class SalaryUpdateDto
    {
        [Required(ErrorMessage = "Salary ID is required.")]
        public int SalaryId { get; set; }

        public int? EmployeeId { get; set; }

        [Range(0, 99999999.99, ErrorMessage = "Salary amount must be between 0 and 99,999,999.99.")]
        public decimal? SalaryAmount { get; set; }

        public DateTime? EffectiveDate { get; set; }
    }
}