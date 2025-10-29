using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Employees
    {
        [Key]
        public int EmployeeId { get; set; }

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email2 { get; set; }

        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [StringLength(15)]
        public string? PhoneNumber2 { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public DateTime? HireDate { get; set; }

        public int? DepartmentId { get; set; }

        public int? JobTitleId { get; set; }

        public int? StatusId { get; set; }

        public int? DefaultSiteId { get; set; }

        [Range(0, 99.99)]
        public decimal? WorkingTime { get; set; } = 8.00m;

        [Range(0, 1)]
        public byte? IsContracted { get; set; } = 0; // 0 = false, 1 = true

        public string? FamilyData { get; set; }

        public string? Comment1 { get; set; }

        public string? Comment2 { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public JobTitle? JobTitle { get; set; }
        public EmploymentStatus? Status { get; set; }
        public int? VacationDays { get; set; }
        public int? FullVacationDays { get; set; }


    }

    public class EmployeesCreateDto
    {
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string? FirstName { get; set; }

        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string? LastName { get; set; }

        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; }

        [StringLength(100, ErrorMessage = "Secondary email cannot exceed 100 characters.")]
        [EmailAddress(ErrorMessage = "Invalid secondary email address.")]
        public string? Email2 { get; set; }

        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters.")]
        public string? PhoneNumber { get; set; }

        [StringLength(15, ErrorMessage = "Secondary phone number cannot exceed 15 characters.")]
        public string? PhoneNumber2 { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string? Address { get; set; }

        public DateTime? HireDate { get; set; }

        public int? DepartmentId { get; set; }

        public int? JobTitleId { get; set; }

        public int? DefaultSiteId { get; set; }

        [Range(0, 99.99, ErrorMessage = "Working time must be between 0 and 99.99 hours.")]
        public decimal? WorkingTime { get; set; } = 8.00m;

        [Range(0, 1, ErrorMessage = "IsContracted must be 0 (false) or 1 (true).")]
        public byte? IsContracted { get; set; } = 0;

        public string? FamilyData { get; set; }

        public string? Comment1 { get; set; }

        public string? Comment2 { get; set; }
        public JobTitle? JobTitle { get; set; }

        public int? StatusId { get; set; }
        public EmploymentStatus? Status { get; set; }
        public int? VacationDays { get; set; }
        public int? FullVacationDays { get; set; }
    }

    public class EmployeesUpdateDto
    {
        [Required(ErrorMessage = "Employee ID is required.")]
        public int EmployeeId { get; set; }

        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string? FirstName { get; set; }

        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string? LastName { get; set; }

        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; }

        [StringLength(100, ErrorMessage = "Secondary email cannot exceed 100 characters.")]
        [EmailAddress(ErrorMessage = "Invalid secondary email address.")]
        public string? Email2 { get; set; }

        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters.")]
        public string? PhoneNumber { get; set; }

        [StringLength(15, ErrorMessage = "Secondary phone number cannot exceed 15 characters.")]
        public string? PhoneNumber2 { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string? Address { get; set; }

        public DateTime? HireDate { get; set; }

        public int? DepartmentId { get; set; }

        public int? JobTitleId { get; set; }

        public int? StatusId { get; set; }

        public int? DefaultSiteId { get; set; }

        [Range(0, 99.99, ErrorMessage = "Working time must be between 0 and 99.99 hours.")]
        public decimal? WorkingTime { get; set; }

        [Range(0, 1, ErrorMessage = "IsContracted must be 0 (false) or 1 (true).")]
        public byte? IsContracted { get; set; }

        public string? FamilyData { get; set; }

        public string? Comment1 { get; set; }

        public string? Comment2 { get; set; }
        public int? VacationDays { get; set; }
        public int? FullVacationDays { get; set; }
    }

}