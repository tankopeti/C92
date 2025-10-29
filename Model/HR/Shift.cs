using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Shift
    {
        [Key]
        public int ShiftId { get; set; }

        public int? PartnerId { get; set; }

        public int? SiteId { get; set; }

        public int? ContactId { get; set; }

        public DateTime? ShiftDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [StringLength(50)]
        public string? ShiftType { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        public string? Notes { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Partner? Partner { get; set; }
        public Site? Site { get; set; }
        public Contact? Contact { get; set; }
        public ICollection<EmployeeShift>? EmployeeShifts { get; set; }
        public ICollection<PartnerShift>? PartnerShifts { get; set; }
        public ICollection<SiteShift>? SiteShifts { get; set; }
    }
        public class ShiftCreateDto
    {
        public int? PartnerId { get; set; }

        public int? SiteId { get; set; }

        public int? ContactId { get; set; }

        public DateTime? ShiftDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [StringLength(50, ErrorMessage = "Shift type cannot exceed 50 characters.")]
        public string? ShiftType { get; set; }

        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters.")]
        public string? Location { get; set; }

        public string? Notes { get; set; }
    }

    public class ShiftUpdateDto
    {
        [Required(ErrorMessage = "Shift ID is required.")]
        public int ShiftId { get; set; }

        public int? PartnerId { get; set; }

        public int? SiteId { get; set; }

        public int? ContactId { get; set; }

        public DateTime? ShiftDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [StringLength(50, ErrorMessage = "Shift type cannot exceed 50 characters.")]
        public string? ShiftType { get; set; }

        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters.")]
        public string? Location { get; set; }

        public string? Notes { get; set; }
    }

}