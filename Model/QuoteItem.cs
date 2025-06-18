using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class QuoteItem
    {
        [Key]
        public int QuoteItemId { get; set; }

        [Required]
        public int QuoteId { get; set; }

        [ForeignKey("QuoteId")]
        public Quote Quote { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [Required]
        public int VatTypeId { get; set; }

        [ForeignKey("VatTypeId")]
        public VatType VatType { get; set; }

        [StringLength(200)]
        public string? ItemDescription { get; set; }

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; } // Stored total

        public QuoteItemDiscount Discount { get; set; } // 1-to-1
    }
}