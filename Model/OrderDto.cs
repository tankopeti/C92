using System;
using System.ComponentModel.DataAnnotations;

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
    public int? SiteId { get; set; }
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
}
        public class SiteDto
    {
        public int SiteId { get; set; }
        public string? Address { get; set; }
    }

    public class OrderItemDto
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public ProductDto Product { get; set; }
        [Range(1, int.MaxValue)]
        public decimal Quantity { get; set; }
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }
        [StringLength(200)]
        public string Description { get; set; }
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }
        // [NotMapped]
        public decimal TotalPrice => Quantity * UnitPrice - (DiscountAmount ?? (DiscountPercentage.HasValue ? (Quantity * UnitPrice * DiscountPercentage.Value / 100) : 0));
    }

public class CreateOrderDto
    {
        public int PartnerId { get; set; }
        public int CurrencyId { get; set; }
        public int? SiteId { get; set; }
        public int? QuoteId { get; set; }
        public int? ContactId { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? OrderType { get; set; }
        public string? CompanyName { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? PaymentTerms { get; set; }
        public string? ShippingMethod { get; set; }
        public string? SalesPerson { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public string? DetailedDescription { get; set; }
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public List<CreateOrderItemDto>? OrderItems { get; set; } = new List<CreateOrderItemDto>();
    }


    public class UpdateOrderDto
    {
        [StringLength(50)]
        public string? OrderNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? Deadline { get; set; }
        [StringLength(1000)]
        public string? Description { get; set; }
        [DataType(DataType.Currency)]
        public decimal? TotalAmount { get; set; }
        [StringLength(100)]
        public string? SalesPerson { get; set; }
        public DateTime? DeliveryDate { get; set; }
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }
        [StringLength(100)]
        public string? CompanyName { get; set; }
        [StringLength(200)]
        public string? Subject { get; set; }
        [StringLength(2000)]
        public string? DetailedDescription { get; set; }
        [StringLength(50)]
        public string? Status { get; set; }
        public int? PartnerId { get; set; }
        public int? SiteId { get; set; }
        public int? CurrencyId { get; set; }
        [StringLength(100)]
        public string? PaymentTerms { get; set; }
        [StringLength(100)]
        public string? ShippingMethod { get; set; }
        [StringLength(50)]
        public string? OrderType { get; set; }
        public List<UpdateOrderItemDto>? OrderItems { get; set; }
        [StringLength(50)]
        public string? ReferenceNumber { get; set; }
        public int? QuoteId { get; set; }
    }

    public class OrderItemResponseDto
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ItemDescription { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? Description { get; set; }
    }

    public class CreateOrderItemDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? Description { get; set; }
        public int OrderId { get; set; }
    }

public class UpdateOrderItemDto
{
    public int OrderItemId { get; set; } // Required to identify item for updates
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
}


}