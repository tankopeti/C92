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
        public int? VatTypeId { get; set; }
        public string? VatTypeName { get; set; }
        public decimal? VatRate { get; set; }
    }

    public class CreateQuoteItemDto
    {
        [Required(ErrorMessage = "QuoteId is required")]
        public int QuoteId { get; set; }

        [Required(ErrorMessage = "ProductId is required")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "UnitPrice is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "UnitPrice must be greater than 0")]
        public decimal UnitPrice { get; set; }

        [StringLength(200, ErrorMessage = "ItemDescription cannot exceed 200 characters")]
        public string? ItemDescription { get; set; }

        [Range(0, 100, ErrorMessage = "DiscountPercentage must be between 0 and 100")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "DiscountAmount must be non-negative")]
        public decimal? DiscountAmount { get; set; }

        [Required(ErrorMessage = "VatTypeId is required")]
        public int VatTypeId { get; set; } // Non-nullable to match QuoteItem
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
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string? ItemDescription { get; set; }
            public decimal? DiscountPercentage { get; set; }
            public decimal? DiscountAmount { get; set; }
            public int VatTypeId { get; set; }
        }

}