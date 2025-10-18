// using System;
// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;

// namespace Cloud9_2.Models
// {
//     public class OrderItem
//     {
//         [Key]
//         public int OrderItemId { get; set; }

//         [Required]
//         [Display(Name = "Rendelés")]
//         public int OrderId { get; set; }

//         [ForeignKey("OrderId")]
//         public Order? Order { get; set; }

//         [StringLength(500)]
//         [Display(Name = "Leírás")]
//         public string? Description { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,4)")]
//         [Display(Name = "Mennyiség")]
//         [Range(0, 999999999999.9999)]
//         public decimal Quantity { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,2)")]
//         [Display(Name = "Egységár")]
//         public decimal UnitPrice { get; set; }

//         [Column(TypeName = "decimal(18,2)")]
//         [Display(Name = "Kedvezmény összeg")]
//         public decimal? DiscountAmount { get; set; }

//         [StringLength(100)]
//         [Display(Name = "Létrehozta")]
//         public string? CreatedBy { get; set; } = "System";

//         [Display(Name = "Létrehozás dátuma")]
//         [DataType(DataType.DateTime)]
//         public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

//         [StringLength(100)]
//         [Display(Name = "Módosította")]
//         public string? ModifiedBy { get; set; } = "System";

//         [Display(Name = "Módosítás dátuma")]
//         [DataType(DataType.DateTime)]
//         public DateTime? ModifiedDate { get; set; } = DateTime.UtcNow;

//         [Display(Name = "Kedvezmény típusa")]
//         public DiscountType? DiscountType { get; set; }

//         [Required]
//         [Display(Name = "Termék")]
//         public int ProductId { get; set; }

//         [ForeignKey("ProductId")]
//         public Product? Product { get; set; }

//         [Display(Name = "ÁFA típus")]
//         public int? VatTypeId { get; set; }

//         [ForeignKey("VatTypeId")]
//         public VatType? VatType { get; set; }

//         [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
//         [Column(TypeName = "decimal(18,2)")]
//         [Display(Name = "Sorösszeg")]
//         public decimal LineTotal { get; private set; }
//     }

//     public class OrderItemDTO
//     {
//         public int OrderItemId { get; set; }
//         public int OrderId { get; set; }
//         public string? Description { get; set; }
//         [Column(TypeName = "decimal(18,4)")]
//         public decimal Quantity { get; set; }
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal UnitPrice { get; set; }
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal? DiscountAmount { get; set; }
//         public string? CreatedBy { get; set; }
//         public DateTime? CreatedDate { get; set; }
//         public string? ModifiedBy { get; set; }
//         public DateTime? ModifiedDate { get; set; }
//         public DiscountType? DiscountType { get; set; }
//         public int ProductId { get; set; }
//         public string? ProductName { get; set; } // Added for related data
//         public int? VatTypeId { get; set; }
//         public decimal? VatRate { get; set; } // Added for related data
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal LineTotal { get; set; } // Added for computed column
//     }

//     public class OrderItemCreateDTO
//     {
//         [Required]
//         public int OrderId { get; set; }

//         [StringLength(500)]
//         public string? Description { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,4)")]
//         [Range(0, 999999999999.9999)]
//         public decimal Quantity { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal UnitPrice { get; set; }

//         [Column(TypeName = "decimal(18,2)")]
//         public decimal? DiscountAmount { get; set; }

//         public DiscountType? DiscountType { get; set; }

//         [Required]
//         public int ProductId { get; set; }

//         public int? VatTypeId { get; set; }
//     }

//     public class OrderItemUpdateDTO
//     {
//         [Required]
//         public int OrderItemId { get; set; }

//         [Required]
//         public int OrderId { get; set; }

//         [StringLength(500)]
//         public string? Description { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,4)")]
//         [Range(0, 999999999999.9999)]
//         public decimal Quantity { get; set; }

//         [Required]
//         [Column(TypeName = "decimal(18,2)")]
//         public decimal UnitPrice { get; set; }

//         [Column(TypeName = "decimal(18,2)")]
//         public decimal? DiscountAmount { get; set; }

//         public DiscountType? DiscountType { get; set; }

//         [Required]
//         public int ProductId { get; set; }

//         public int? VatTypeId { get; set; }
//     }

//     public enum DiscountType
//     {
//         None = 1,
//         CustomDiscountPercentage = 2,
//         CustomDiscountAmount = 3,
//         PartnerPrice = 4,
//         VolumeDiscount = 5,
//         ListPrice = 6
//     }
// }