using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class OrderItemDiscount
    {
        [Key]
        public int OrderItemDiscountId { get; set; }

        [Required]
        [Display(Name = "Tétel azonosító")]
        public int OrderItemId { get; set; }

        [Required]
        [Display(Name = "Kedvezmény típusa")]
        public DiscountType DiscountType { get; set; } // Enum: 1–6

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Kedvezmény %")]
        public decimal? DiscountPercentage { get; set; } // For CustomDiscountPercentage

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Kedvezmény összeg")]
        public decimal? DiscountAmount { get; set; } // For CustomDiscountAmount

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Alapár")]
        public decimal? BasePrice { get; set; } // Original price before discount

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Partner ár")]
        public decimal? PartnerPrice { get; set; } // For PartnerPrice

        [Display(Name = "Mennyiségi küszöb")]
        public int? VolumeThreshold { get; set; } // For VolumeDiscount

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Mennyiségi ár")]
        public decimal? VolumePrice { get; set; } // For VolumeDiscount

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Listaár")]
        public decimal? ListPrice { get; set; } // Added by migration

        [ForeignKey("OrderItemId")]
        public OrderItem? OrderItem { get; set; }
    }

    public class OrderItemDiscountDTO
{
    public int OrderItemDiscountId { get; set; }
    public int OrderItemId { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal? PartnerPrice { get; set; }
    public int? VolumeThreshold { get; set; }
    public decimal? VolumePrice { get; set; }
    public decimal? ListPrice { get; set; }
}

public class OrderItemDiscountCreateDTO
{
    [Required]
    public int OrderItemId { get; set; }

    [Required]
    public DiscountType DiscountType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountPercentage { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? BasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PartnerPrice { get; set; }

    public int? VolumeThreshold { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? VolumePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ListPrice { get; set; }
}

public class OrderItemDiscountUpdateDTO
{
    [Required]
    public int OrderItemDiscountId { get; set; }

    [Required]
    public int OrderItemId { get; set; }

    [Required]
    public DiscountType DiscountType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountPercentage { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? BasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PartnerPrice { get; set; }

    public int? VolumeThreshold { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? VolumePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ListPrice { get; set; }
}


}