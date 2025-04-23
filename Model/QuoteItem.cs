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
        public Quote? Quote { get; set; }

        [Required]
        [Display(Name = "Termék")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [StringLength(200)]
        [Display(Name = "Tétel leírása")]
        public string? ItemDescription { get; set; } // Optional, so keep nullable

        [Required]
        [Display(Name = "Mennyiség")]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Display(Name = "Egységár")]
        public decimal UnitPrice { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Összesen")]
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}