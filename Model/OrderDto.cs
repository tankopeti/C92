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
        public string? Status { get; set; }
        public string? SalesPerson { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? CompanyName { get; set; }
        public string? Subject { get; set; }
        public string? PaymentTerms { get; set; }
        public string? ShippingMethod { get; set; }
        public string? OrderType { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Description { get; set; }
        public string? DetailedDescription { get; set; }
        public int PartnerId { get; set; }
        public int? SiteId { get; set; } // Nullable to match Order
        public int CurrencyId { get; set; }
        public int? QuoteId { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public List<OrderItemDto>? OrderItems { get; set; }
        public CurrencyDto? Currency { get; set; }
        public PartnerDto? Partner { get; set; }

    }

    public class OrderItemDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; } // Match OrderItem's int ProductId
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int? DiscountTypeId { get; set; } // Renamed from DiscountType
        public decimal? DiscountAmount { get; set; }
        public int? VatTypeId { get; set; } // Added for client compatibility
        public decimal? VatRate { get; set; }
        public string? Description { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public ProductDto? Product { get; set; }
    }
    // public class SiteDto
    // {
    //     public int SiteId { get; set; }
    //     public string? Address { get; set; }
    // }
    
    

}