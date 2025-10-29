using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class PartnerProductPrice
    {
        [Key]
        public int PartnerProductPriceId { get; set; }

        [Required]
        [Display(Name = "Partner azonosító")]
        public int PartnerId { get; set; }

        [Required]
        [Display(Name = "Termék azonosító")]
        public int ProductId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Egyedi egységár")]
        public decimal PartnerUnitPrice { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public string CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("PartnerId")]
        public Partner? Partner { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}