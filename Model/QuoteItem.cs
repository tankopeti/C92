using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class QuoteItem
    {
        [Key]
        public int QuoteItemId { get; set; }

        [Required]
        [Display(Name = "Árajánlat azonosító")]
        public int QuoteId { get; set; }

        [Required]
        [Display(Name = "Termék azonosító")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "ÁFA típus")]
        public int VatTypeId { get; set; }

        [StringLength(200)]
        [Display(Name = "Tétel leírása")]
        public string? ItemDescription { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Mennyiség")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Nettó kedvezményes ár")]
        public decimal NetDiscountedPrice { get; set; } // Renamed from UnitPrice

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Összesen")]
        public decimal TotalPrice { get; set; } // Stored total

        // Navigation properties
        [ForeignKey("QuoteId")]
        public Quote? Quote { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [ForeignKey("VatTypeId")]
        public VatType? VatType { get; set; }

        [ForeignKey("QuoteItemId")]
        public QuoteItemDiscount? Discount { get; set; } // 1-to-1
    }
}