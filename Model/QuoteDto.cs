using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class QuoteDto
    {
        public int QuoteId { get; set; }
        public string QuoteNumber { get; set; }
        public int PartnerId { get; set; }
        public DateTime? QuoteDate { get; set; }
        public string Status { get; set; }
        public decimal? TotalAmount { get; set; }
    }

    public class CreateQuoteDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "PartnerId must be a positive number")]
        public int PartnerId { get; set; }

        public DateTime? QuoteDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Az összeg nem lehet negatív")]
        public decimal? TotalAmount { get; set; }
    }

    public class UpdateQuoteDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "PartnerId must be a positive number")]
        public int PartnerId { get; set; }

        public DateTime? QuoteDate { get; set; }

        [Required]
        [RegularExpression("^(Tervezet|Elküldve|Elfogadva|Elutasítva)$", ErrorMessage = "Nem létező státusz")]
        public string Status { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Az összeg nem lehet negatív")]
        public decimal? TotalAmount { get; set; }
    }

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
        public string ItemDescription { get; set; }

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
        public string ItemDescription { get; set; }

        [Range(0, 100, ErrorMessage = "A százaléknak 0 és 100 közé kell esnie")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Az összeg nem lehet negatív")]
        public decimal? DiscountAmount { get; set; }
    }

    public class UpdateQuoteItemDto
    {
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
        public string ItemDescription { get; set; }

        [Range(0, 100, ErrorMessage = "A százaléknak 0 és 100 közé kell esnie")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Az összeg nem lehet negatív")]
        public decimal? DiscountAmount { get; set; }
    }
}