using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class ProductGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductGroupId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Csoport neve maximum 100 karakter lehet")]
        [Display(Name = "Csoport neve")]
        public string Name { get; set; }

        [Display(Name = "Termékek")]
        public List<ProductGroupProduct> ProductGroupProducts { get; set; } = new List<ProductGroupProduct>();
    }
}