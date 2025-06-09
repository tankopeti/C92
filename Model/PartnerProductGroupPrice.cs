using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class PartnerProductGroupPrice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PartnerProductGroupPriceId { get; set; }

        [Required]
        public int PartnerId { get; set; }

        [ForeignKey("PartnerId")]
        [Display(Name = "Partner")]
        public Partner Partner { get; set; }

        [Required]
        public int ProductGroupId { get; set; }

        [ForeignKey("ProductGroupId")]
        [Display(Name = "Termék csoport")]
        public ProductGroup ProductGroup { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Display(Name = "Egyedi egységár")]
        public decimal UnitPrice { get; set; } // Custom price for all products in this group
    }
}