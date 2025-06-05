using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cloud9_2.Models
{
    [PrimaryKey(nameof(ProductId), nameof(ProductGroupId))]
    public class ProductGroupProduct
    {
        public int ProductId { get; set; }
        public int ProductGroupId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [ForeignKey("ProductGroupId")]
        public ProductGroup ProductGroup { get; set; }
    }
}