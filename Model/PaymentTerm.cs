using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class PaymentTerm
    {
        [Key]
        public int PaymentTermId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Fizetési feltétel")]
        public string TermName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Leírás")]
        public string? Description { get; set; }

        [Display(Name = "Fizetési határidő (napok)")]
        public int? DaysDue { get; set; }

        [StringLength(100)]
        [Display(Name = "Létrehozta")]
        public string? CreatedBy { get; set; } = "System";

        [Display(Name = "Létrehozás dátuma")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        [Display(Name = "Módosította")]
        public string? ModifiedBy { get; set; } = "System";

        [Display(Name = "Módosítás dátuma")]
        [DataType(DataType.DateTime)]
        public DateTime? ModifiedDate { get; set; } = DateTime.UtcNow;

        public ICollection<Order>? Orders { get; set; }
    }

    public class PaymentTermDTO
{
    public int PaymentTermId { get; set; }
    public string TermName { get; set; }
    public string? Description { get; set; }
    public int? DaysDue { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class PaymentTermCreateDTO
{
    [Required]
    [StringLength(100)]
    public string TermName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int? DaysDue { get; set; }
}

public class PaymentTermUpdateDTO
{
    [Required]
    public int PaymentTermId { get; set; }

    [Required]
    [StringLength(100)]
    public string TermName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int? DaysDue { get; set; }
}

}