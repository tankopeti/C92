using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cloud9_2.Models;

public class PartnerProductPrice
{
    [Key]
    public int PartnerProductPriceId { get; set; }
    [Required]
    public int PartnerId { get; set; }
    [Required]
    public int ProductId { get; set; }
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Egyedi egységár")]
    public decimal PartnerUnitPrice { get; set; }

    [ForeignKey("PartnerId")]
    public Partner Partner { get; set; }
    [ForeignKey("ProductId")]
    public Product Product { get; set; }
}