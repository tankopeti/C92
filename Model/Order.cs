using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [StringLength(100)]
        [Display(Name = "Rendelésszám")]
        public string? OrderNumber { get; set; }

        [Display(Name = "Rendelés dátuma")]
        [DataType(DataType.Date)]
        public DateTime? OrderDate { get; set; }

        [Display(Name = "Határidő")]
        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        [StringLength(500)]
        [Display(Name = "Leírás")]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Összesen")]
        public decimal? TotalAmount { get; set; }

        [StringLength(100)]
        [Display(Name = "Értékesítő")]
        public string? SalesPerson { get; set; }

        [Display(Name = "Szállítási dátum")]
        [DataType(DataType.Date)]
        public DateTime? DeliveryDate { get; set; }

        [Display(Name = "Tervezett szállítás")]
        [DataType(DataType.DateTime)]
        public DateTime? PlannedDelivery { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Kedvezmény %")]
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Kedvezmény összeg")]
        public decimal? DiscountAmount { get; set; }

        [StringLength(100)]
        [Display(Name = "Cég neve")]
        public string? CompanyName { get; set; }

        [StringLength(200)]
        [Display(Name = "Tárgy")]
        public string? Subject { get; set; }

        [Display(Name = "Részletes leírás")]
        [DataType(DataType.MultilineText)]
        public string? DetailedDescription { get; set; }

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

        [StringLength(50)]
        [Display(Name = "Státusz")]
        public string? Status { get; set; } = "Pending";

        [Required]
        [Display(Name = "Partner")]
        public int PartnerId { get; set; }

        [ForeignKey("PartnerId")]
        public Partner Partner { get; set; }

        [Display(Name = "Partner telephely")]
        public int? SiteId { get; set; }

        [ForeignKey("SiteId")]
        public Site? Site { get; set; }

        [Required]
        [Display(Name = "Pénznem")]
        public int CurrencyId { get; set; }

        [ForeignKey("CurrencyId")]
        public Currency? Currency { get; set; }

        [Display(Name = "Szállítási mód")]
        public int? ShippingMethodId { get; set; }

        [ForeignKey("ShippingMethodId")]
        public OrderShippingMethod? ShippingMethod { get; set; }

        [Display(Name = "Fizetési feltételek")]
        public int? PaymentTermId { get; set; }

        [ForeignKey("PaymentTermId")]
        public PaymentTerm? PaymentTerm { get; set; }

        [Display(Name = "Kapcsolattartó")]
        public int? ContactId { get; set; }

        [ForeignKey("ContactId")]
        public Contact? Contact { get; set; }

        [StringLength(50)]
        [Display(Name = "Rendelés típusa")]
        public string? OrderType { get; set; }

        [Display(Name = "Tételek")]
        public List<OrderItem>? OrderItems { get; set; } = new List<OrderItem>();

        [StringLength(100)]
        [Display(Name = "Referenciaszám")]
        public string? ReferenceNumber { get; set; }

        [Display(Name = "Árajánlat azonosító")]
        public int? QuoteId { get; set; }

        [ForeignKey("QuoteId")]
        public Quote? Quote { get; set; }

        [Display(Name = "Törölve")]
        public bool? IsDeleted { get; set; } = false;

        [Display(Name = "Rendelés státusz típus")]
        public int? OrderStatusTypes { get; set; }

        // [ForeignKey("OrderStatusTypes")]
        // public OrderStatusType? OrderStatusType { get; set; }

        public ICollection<CustomerCommunication>? Communications { get; set; }
    }

    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        [Required]
        [Display(Name = "Rendelés")]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [StringLength(500)]
        [Display(Name = "Leírás")]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Mennyiség")]
        [Range(0, 999999999999.9999)]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Egységár")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Kedvezmény összeg")]
        public decimal? DiscountAmount { get; set; }

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

        [Display(Name = "Kedvezmény típusa")]
        public DiscountType? DiscountType { get; set; }

        [Required]
        [Display(Name = "Termék")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Display(Name = "ÁFA típus")]
        public int? VatTypeId { get; set; }

        [ForeignKey("VatTypeId")]
        public VatType? VatType { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Sorösszeg")]
        public decimal LineTotal { get; private set; }
    }

    public class OrderDTO
    {
        public int OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? Deadline { get; set; }
        public string? Description { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmount { get; set; }
        public string? SalesPerson { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DateTime? PlannedDelivery { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountPercentage { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountAmount { get; set; }
        public string? CompanyName { get; set; }
        public string? Subject { get; set; }
        public string? DetailedDescription { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? Status { get; set; }
        public int PartnerId { get; set; }
        public string? PartnerName { get; set; } // Added for related data
        public int? SiteId { get; set; }
        public string? SiteName { get; set; } // Added for related data
        public int CurrencyId { get; set; }
        public string? CurrencyCode { get; set; } // Added for related data
        public int? ShippingMethodId { get; set; }
        public string? ShippingMethodName { get; set; } // Added for related data
        public int? PaymentTermId { get; set; }
        public string? PaymentTermName { get; set; } // Added for related data
        public int? ContactId { get; set; }
        public string? ContactName { get; set; } // Added for related data
        public string? OrderType { get; set; }
        public List<OrderItemDTO>? OrderItems { get; set; }
        public string? ReferenceNumber { get; set; }
        public int? QuoteId { get; set; }
        public bool? IsDeleted { get; set; }
        public int? OrderStatusTypes { get; set; }
        public string? OrderStatusTypeName { get; set; } // Added for related data
    }

    public class OrderItemDTO
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public string? Description { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountAmount { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DiscountType? DiscountType { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? VatTypeId { get; set; }
        public decimal? VatRate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }
    }

    public class OrderCreateDTO
    {
        [StringLength(100)]
        public string? OrderNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? OrderDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal? TotalAmount { get; set; }

        [StringLength(100)]
        public string? SalesPerson { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DeliveryDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? PlannedDelivery { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }

        [StringLength(100)]
        public string? CompanyName { get; set; }

        [StringLength(200)]
        public string? Subject { get; set; }

        [DataType(DataType.MultilineText)]
        public string? DetailedDescription { get; set; }

        [StringLength(50)]
        public string? Status { get; set; } = "Pending";

        [Required]
        public int PartnerId { get; set; }

        public int? SiteId { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        public int? ShippingMethodId { get; set; }

        public int? PaymentTermId { get; set; }

        public int? ContactId { get; set; }

        [StringLength(50)]
        public string? OrderType { get; set; }

        public List<OrderItemCreateDTO>? OrderItems { get; set; }

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        public int? QuoteId { get; set; }

        public bool? IsDeleted { get; set; } = false;

        public int? OrderStatusTypes { get; set; }
    }

    public class OrderItemCreateDTO
    {
        [Required]
        public int OrderId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        [Range(0, 999999999999.9999)]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountAmount { get; set; }

        public DiscountType? DiscountType { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int? VatTypeId { get; set; }
    }

    public class OrderUpdateDTO
    {
        [Required]
        public int OrderId { get; set; }

        [StringLength(100)]
        public string? OrderNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? OrderDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal? TotalAmount { get; set; }

        [StringLength(100)]
        public string? SalesPerson { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DeliveryDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? PlannedDelivery { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }

        [StringLength(100)]
        public string? CompanyName { get; set; }

        [StringLength(200)]
        public string? Subject { get; set; }

        [DataType(DataType.MultilineText)]
        public string? DetailedDescription { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [Required]
        public int PartnerId { get; set; }

        public int? SiteId { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        public int? ShippingMethodId { get; set; }

        public int? PaymentTermId { get; set; }

        public int? ContactId { get; set; }

        [StringLength(50)]
        public string? OrderType { get; set; }

        public List<OrderItemUpdateDTO>? OrderItems { get; set; }

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        public int? QuoteId { get; set; }

        public bool? IsDeleted { get; set; }

        public int? OrderStatusTypes { get; set; }
    }

    public class OrderItemUpdateDTO
    {
        [Required]
        public int OrderItemId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        [Range(0, 999999999999.9999)]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountAmount { get; set; }

        public DiscountType? DiscountType { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int? VatTypeId { get; set; }
    }

    public enum DiscountType
    {
        None = 1,
        CustomDiscountPercentage = 2,
        CustomDiscountAmount = 3,
        PartnerPrice = 4,
        VolumeDiscount = 5,
        ListPrice = 6
    }
}