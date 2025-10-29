using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class SiteShift
    {
        [Key]
        public int SiteId { get; set; }

        [Key]
        public int ShiftId { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Site? Site { get; set; }
        public Shift? Shift { get; set; }
    }
    public class SiteShiftCreateDto
    {
        [Required(ErrorMessage = "Site ID is required.")]
        public int SiteId { get; set; }

        [Required(ErrorMessage = "Shift ID is required.")]
        public int ShiftId { get; set; }
    }

    public class SiteShiftUpdateDto
    {
        [Required(ErrorMessage = "Site ID is required.")]
        public int SiteId { get; set; }

        [Required(ErrorMessage = "Shift ID is required.")]
        public int ShiftId { get; set; }

        [Required(ErrorMessage = "New Site ID is required for update.")]
        public int NewSiteId { get; set; }

        [Required(ErrorMessage = "New Shift ID is required for update.")]
        public int NewShiftId { get; set; }
    }
}