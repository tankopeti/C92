using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class QuoteDto
    {
        public int QuoteId { get; set; }
        public string QuoteNumber { get; set; }
        public int PartnerId { get; set; }
        public int CurrencyId { get; set; }
        public CurrencyDto? Currency { get; set; } // Add this property
        public DateTime? QuoteDate { get; set; }
        public string Status { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? SalesPerson { get; set; }
        public DateTime? ValidityDate { get; set; }
        public string Subject { get; set; }
        public string? Description { get; set; }
        public string? DetailedDescription { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? CompanyName { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ReferenceNumber { get; set; }
        public List<QuoteItemDto> Items { get; set; } = new List<QuoteItemDto>();
    }



    public class CreateQuoteDto
    {
        public string QuoteNumber { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "PartnerId must be a positive number")]
        public int PartnerId { get; set; }

        public DateTime? QuoteDate { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Az összeg nem lehet negatív")]
        public decimal? TotalAmount { get; set; }

        [StringLength(100)]
        public string? SalesPerson { get; set; }

        public DateTime? ValidityDate { get; set; }

        [Required(ErrorMessage = "A státusz megadása kötelező")]
        [StringLength(50)]
        public string Status { get; set; } = "Draft";  // ✅ Default value

        [Required(ErrorMessage = "A tárgy megadása kötelező")]
        [StringLength(200)]
        public string Subject { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(1000)]
        public string? DetailedDescription { get; set; }

        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DiscountAmount { get; set; }

        [StringLength(100)]
        public string? CompanyName { get; set; }

        public string? CreatedBy { get; set; } = "System";

        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

        public string? ModifiedBy { get; set; } = "System";

        public DateTime? ModifiedDate { get; set; } = DateTime.UtcNow;

        public string? ReferenceNumber { get; set; }

        public List<QuoteItemDto> Items { get; set; } = new List<QuoteItemDto>();
    }


public class UpdateQuoteDto
{
    [Required(ErrorMessage = "Az árajánlat száma kötelező")]
    [StringLength(50, ErrorMessage = "Az árajánlat száma maximum 50 karakter lehet")]
    public string QuoteNumber { get; set; }

    [Required(ErrorMessage = "A partner azonosító kötelező")]
    public int PartnerId { get; set; }

    [Required]
    public int CurrencyId { get; set; }

    [Required(ErrorMessage = "Az árajánlat dátuma kötelező")]
    public DateTime? QuoteDate { get; set; }

    [Required(ErrorMessage = "A státusz kötelező")]
    public string Status { get; set; }

    public decimal? TotalAmount { get; set; }

    [StringLength(100, ErrorMessage = "Az értékesítő neve maximum 100 karakter lehet")]
    public string? SalesPerson { get; set; }

    public DateTime? ValidityDate { get; set; }

    [Required(ErrorMessage = "A tárgy kötelező")]
    [StringLength(200, ErrorMessage = "A tárgy maximum 200 karakter lehet")]
    public string Subject { get; set; }

    [StringLength(500, ErrorMessage = "A leírás maximum 500 karakter lehet")]
    public string? Description { get; set; }

    [StringLength(1000, ErrorMessage = "A részletes leírás maximum 1000 karakter lehet")]
    public string? DetailedDescription { get; set; }

    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
}

        public class PartnerDto
    {
        public int PartnerId { get; set; }
        public string? Name { get; set; }
    }

    public class UpdateQuoteResponseDto
    {
        public int QuoteId { get; set; }
        public string QuoteNumber { get; set; }
        public int PartnerId { get; set; }
        public int CurrencyId { get; set; }
        public DateTime? QuoteDate { get; set; }
        public string Status { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? SalesPerson { get; set; }
        public DateTime? ValidityDate { get; set; }
        public string Subject { get; set; }
        public string? Description { get; set; }
        public string? DetailedDescription { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        
    }

    public class ConvertQuoteToOrderDto
    {
        [Required(ErrorMessage = "CurrencyId is required")]
        public int CurrencyId { get; set; }
        public int? SiteId { get; set; }
        [StringLength(100)]
        public string PaymentTerms { get; set; }
        [StringLength(100)]
        public string ShippingMethod { get; set; }
        [StringLength(50)]
        public string OrderType { get; set; }
    }

}