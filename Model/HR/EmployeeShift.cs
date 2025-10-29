using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class EmployeeShift
    {
        [Key]
        public int EmployeeId { get; set; }

        [Key]
        public int ShiftId { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Employees? Employee { get; set; }
        public Shift? Shift { get; set; }
    }
    public class EmployeeShiftCreateDto
    {
        [Required(ErrorMessage = "Employee ID is required.")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Shift ID is required.")]
        public int ShiftId { get; set; }
    }

    public class EmployeeShiftUpdateDto
    {
        [Required(ErrorMessage = "Employee ID is required.")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Shift ID is required.")]
        public int ShiftId { get; set; }

        [Required(ErrorMessage = "New Employee ID is required for update.")]
        public int NewEmployeeId { get; set; }

        [Required(ErrorMessage = "New Shift ID is required for update.")]
        public int NewShiftId { get; set; }
    }

}