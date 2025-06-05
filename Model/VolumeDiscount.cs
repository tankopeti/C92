using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class VolumeDiscount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolumeDiscountId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        [Display(Name = "Termék")]
        public Product Product { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Minimális mennyiség pozitív kell legyen")]
        [Display(Name = "Minimális mennyiség")]
        public int MinQuantity { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Kedvezmény százaléka 0 és 100 között kell legyen")]
        [Display(Name = "Kedvezmény (%)")]
        public decimal DiscountPercentage { get; set; } // e.g., 10.00 for 10%
    }
}