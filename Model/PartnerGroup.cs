using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class PartnerGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PartnerGroupId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Csoport neve maximum 100 karakter lehet")]
        [Display(Name = "Csoport neve")]
        public string PartnerGroupName { get; set; }

        [Range(0, 100, ErrorMessage = "Kedvezmény százaléka 0 és 100 között kell legyen")]
        [Display(Name = "Kedvezmény (%)")]
        public decimal? DiscountPercentage { get; set; } // e.g., 5.00 for 5%
        public List<Partner> Partners { get; set; } = new List<Partner>();
    }
}