using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class PartnerShift
    {
        [Key]
        public int PartnerId { get; set; }

        [Key]
        public int ShiftId { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Partner? Partner { get; set; }
        public Shift? Shift { get; set; }
    }
    public class PartnerShiftCreateDto
    {
        [Required(ErrorMessage = "Partner ID is required.")]
        public int PartnerId { get; set; }

        [Required(ErrorMessage = "Shift ID is required.")]
        public int ShiftId { get; set; }
    }

    public class PartnerShiftUpdateDto
    {
        [Required(ErrorMessage = "Partner ID is required.")]
        public int PartnerId { get; set; }

        [Required(ErrorMessage = "Shift ID is required.")]
        public int ShiftId { get; set; }

        [Required(ErrorMessage = "New Partner ID is required for update.")]
        public int NewPartnerId { get; set; }

        [Required(ErrorMessage = "New Shift ID is required for update.")]
        public int NewShiftId { get; set; }
    }
}