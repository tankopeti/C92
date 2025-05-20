using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{

public class QuoteItemDto
{
    public int QuoteItemId { get; set; }
    public int QuoteId { get; set; }
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a positive number")]
    public int ProductId { get; set; }
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "A mennyiség nem lehet negatív")]
    public decimal Quantity { get; set; }
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Az ár nem lehet negatív")]
    public decimal UnitPrice { get; set; }

    [StringLength(200)]
    public string? ItemDescription { get; set; }

    [Range(0, 100, ErrorMessage = "A százaléknak 0 és 100 közé kell esnie")]
    public decimal? DiscountPercentage { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Az összeg nem lehet negatív")]
    public decimal? DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
}

    public class CreateQuoteItemDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a positive number")]
        public int ProductId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Az mennyiség nem lehet negatív")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Az ár nem lehet negatív")]
        public decimal UnitPrice { get; set; }

        [StringLength(200)]
        public string? ItemDescription { get; set; }

        [Range(0, 100, ErrorMessage = "A százaléknak 0 és 100 közé kell esnie")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Az összeg nem lehet negatív")]
        public decimal? DiscountAmount { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "QuoteId must be a positive number")]
        public int? QuoteId { get; set; }
    }

    public class UpdateQuoteItemDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a positive number")]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "QuoteId must be a positive number")]
        public int? QuoteId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "A mennyiség nem lehet negatív")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Az ár nem lehet negatív")]
        public decimal UnitPrice { get; set; }

        [StringLength(200)]
        public string? ItemDescription { get; set; }

        [Range(0, 100, ErrorMessage = "A százaléknak 0 és 100 közé kell esnie")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Az összeg nem lehet negatív")]
        public decimal? DiscountAmount { get; set; }
    }
    public class QuoteItemCreateDto
    {
        public int QuoteId { get; set; }
        public int ProductId { get; set; }
        public string Subject { get; set; }
        public string? Description { get; set; }
        public string? DetailedDescription { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "A mennyiség nem lehet negatív")]
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? ItemDescription { get; set; } // Nullable
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "PartnerId must be a positive number")]
        public int PartnerId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "CurrencyId must be a positive number")]
        public int CurrencyId { get; set; }
        public DateTime? QuoteDate { get; set; }
        public string? SalesPerson { get; set; } // Nullable
        public DateTime? ValidityDate { get; set; }
        public string? Status { get; set; }
        public List<QuoteItemDto> QuoteItems { get; set; }
    }

public class QuoteItemResponseDto
{
    public int QuoteItemId { get; set; }
    public int QuoteId { get; set; }
    public int ProductId { get; set; }
    public ProductDto Product { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [StringLength(200)]
    public string? ItemDescription { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal QuoteTotalAmount { get; set; }
}

}