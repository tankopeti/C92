using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{

    public class OrderDto
    {
        public int OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? Deadline { get; set; }
        public string? Description { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? SalesPerson { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public decimal? DiscountPercentage { get; set; }
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
        public PartnerDto? Partner { get; set; }
        public int SiteId { get; set; }
        public SiteDto? Site { get; set; }
        public int CurrencyId { get; set; }
        public CurrencyDto? Currency { get; set; }
        public string? PaymentTerms { get; set; }
        public string? ShippingMethod { get; set; }
        public string? OrderType { get; set; }
        public List<OrderItemDto>? OrderItems { get; set; }
        public string? ReferenceNumber { get; set; }
        public int? QuoteId { get; set; }
        public QuoteDto? Quote { get; set; }

        public static implicit operator OrderDto(Order v)
        {
            throw new NotImplementedException();
        }
    }

    public class SiteDto
    {
        public int SiteId { get; set; }
        public string? Address { get; set; }
    }

    public class CreateOrderDto
    {
        [Required]
        public int PartnerId { get; set; }
        [Required]
        public int CurrencyId { get; set; }
        [Required]
        public int SiteId { get; set; }
        public int? QuoteId { get; set; }
        public string? OrderNumber { get; set; } // Server generates TestOrder-XXXX-YYYY
        [Required]
        public DateTime OrderDate { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime? DeliveryDate { get; set; }
        [StringLength(50)]
        public string? ReferenceNumber { get; set; }
        [StringLength(50)]
        public string? OrderType { get; set; }
        [StringLength(100)]
        public string? CompanyName { get; set; }
        [DataType(DataType.Currency)]
        public decimal? TotalAmount { get; set; }
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }
        [StringLength(200)]
        public string? PaymentTerms { get; set; }
        [StringLength(100)]
        public string? ShippingMethod { get; set; }
        [StringLength(100)]
        public string? SalesPerson { get; set; }
        [StringLength(200)]
        public string? Subject { get; set; }
        [StringLength(500)]
        public string? Description { get; set; }
        [StringLength(2000)]
        public string? DetailedDescription { get; set; }
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft";
        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; } = "System";
        [Required]
        public DateTime CreatedDate { get; set; }
        [StringLength(100)]
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public List<CreateOrderItemDto> OrderItems { get; set; } = new List<CreateOrderItemDto>();
    }


    public class OrderItemDto
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public ProductDto? Product { get; set; } // Nullable to fix CS8618
        [Range(1, int.MaxValue)]
        public decimal Quantity { get; set; }
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }
        [StringLength(200)]
        public string? Description { get; set; } // Nullable to fix CS8618
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }
        public OrderItemDiscountDto? OrderItemDiscount { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice - (DiscountAmount ?? (DiscountPercentage.HasValue ? (Quantity * UnitPrice * DiscountPercentage.Value / 100) : 0));
    }

    public class OrderItemDiscountDto
    {
        public int OrderItemDiscountId { get; set; }
        public int OrderItemId { get; set; }
        [Required]
        public int DiscountTypeId { get; set; }
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }
        [DataType(DataType.Currency)]
        public decimal? BasePrice { get; set; }
        [DataType(DataType.Currency)]
        public decimal? PartnerPrice { get; set; }
        public int? VolumeThreshold { get; set; }
        [DataType(DataType.Currency)]
        public decimal? VolumePrice { get; set; }
        [DataType(DataType.Currency)]
        public decimal? ListPrice { get; set; }
    }

    public class CreateOrderItemDto
    {
        [Required]
        public int ProductId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public decimal Quantity { get; set; }
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }
        [StringLength(200)]
        public string? Description { get; set; }
        [Required]
        public int OrderId { get; set; }
        public CreateOrderItemDiscountDto? OrderItemDiscount { get; set; }
    }

    public class CreateOrderItemDiscountDto
    {
        [Required]
        public int DiscountTypeId { get; set; }
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }
        [DataType(DataType.Currency)]
        public decimal? BasePrice { get; set; }
        [DataType(DataType.Currency)]
        public decimal? PartnerPrice { get; set; }
        public int? VolumeThreshold { get; set; }
        [DataType(DataType.Currency)]
        public decimal? VolumePrice { get; set; }
        [DataType(DataType.Currency)]
        public decimal? ListPrice { get; set; }
    }

    public class OrderItemResponseDto
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Description { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public int? DiscountTypeId { get; set; }
        public OrderItemDiscountResponseDto? OrderItemDiscount { get; set; }
    }

    public class OrderItemDiscountResponseDto
    {
        public int OrderItemDiscountId { get; set; }
        public int OrderItemId { get; set; }
        [Required]
        public int DiscountTypeId { get; set; }
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }
        [DataType(DataType.Currency)]
        public decimal? BasePrice { get; set; }
        [DataType(DataType.Currency)]
        public decimal? PartnerPrice { get; set; }
        public int? VolumeThreshold { get; set; }
        [DataType(DataType.Currency)]
        public decimal? VolumePrice { get; set; }
        [DataType(DataType.Currency)]
        public decimal? ListPrice { get; set; }
    }

    public class UpdateOrderItemDto
    {
        public int OrderItemId { get; set; }
        public int? ProductId { get; set; }
        [Range(1, double.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public decimal? Quantity { get; set; }
        [DataType(DataType.Currency)]
        public decimal? UnitPrice { get; set; }
        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
        public string? Description { get; set; }
        [Range(0, 200, ErrorMessage = "Discount percentage must be between 0 and 200.")]
        public decimal? DiscountPercentage { get; set; }
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }
        public int? DiscountTypeId { get; set; }
        public CreateOrderItemDiscountDto? OrderItemDiscount { get; set; }
    }

}