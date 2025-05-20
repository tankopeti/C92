using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        [Required]
        [Display(Name = "Rendelés")]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [StringLength(200)]
        [Display(Name = "Tétel megnevezése")]
        public string? ItemName { get; set; }

        [StringLength(500)]
        [Display(Name = "Leírás")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Mennyiség")]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Required]
        [Display(Name = "Egységár")]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Összesen")]
        public decimal? TotalPrice { get; set; }

        [Display(Name = "Kedvezmény %")]
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Kedvezmény összeg")]
        public decimal? DiscountAmount { get; set; }

        [StringLength(50)]
        [Display(Name = "Mértékegység")]
        public string? UnitOfMeasure { get; set; }

        [StringLength(100)]
        [Display(Name = "Létrehozta")]
        public string? CreatedBy { get; set; } = "System";

        [Display(Name = "Létrehozás dátuma")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        [Display(Name = "Módosította")]
        public string? ModifiedBy { get; set; } = "System";

        [Display(Name = "Módosítás dátuma")]
        [DataType(DataType.DateTime)]
        public DateTime? ModifiedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Termék")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}