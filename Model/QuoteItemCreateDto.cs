using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class QuoteItemCreateDto
    {
        public int QuoteId { get; set; }
        public int ProductId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string DetailedDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? ItemDescription { get; set; } // Nullable
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public int PartnerId { get; set; }
        public int CurrencyId { get; set; }
        public DateTime? QuoteDate { get; set; }
        public string? SalesPerson { get; set; } // Nullable
        public DateTime? ValidityDate { get; set; }
        public string? Status { get; set; }
        public List<QuoteItemDto> QuoteItems { get; set; }
    }
    // public class QuoteItemDto
    // {
    //     public int QuoteId { get; set; }
    //     public int ProductId { get; set; }
    //     public decimal Quantity { get; set; }
    //     public decimal UnitPrice { get; set; }
    //     public string ItemDescription { get; set; }
    //     public decimal? DiscountPercentage { get; set; }
    //     public decimal? DiscountAmount { get; set; }
    // }
}