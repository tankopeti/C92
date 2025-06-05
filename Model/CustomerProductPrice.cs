using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class CustomerProductPrice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerProductPriceId { get; set; }

        [Required]
        public int PartnerId { get; set; }

        [ForeignKey("PartnerId")]
        [Display(Name = "Partner")]
        public Partner Partner { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        [Display(Name = "Termék")]
        public Product Product { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Display(Name = "Egyedi egységár")]
        public decimal UnitPrice { get; set; } // Custom price for this partner and product
    }
}