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
        public string SalesPerson { get; set; }
        public DateTime? ValidityDate { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string DetailedDescription { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public List<QuoteItemDto> Items { get; set; } = new List<QuoteItemDto>(); // Add Items
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

        [Required]
        [MaxLength(100, ErrorMessage = "Quote number cannot exceed 100 characters")]
        public string QuoteNumber { get; set; }

        public DateTime? QuoteDate { get; set; }

        [Required]
        [MaxLength(100, ErrorMessage = "SalesPerson name cannot exceed 100 characters")]
        public string SalesPerson { get; set; }

        public DateTime? ValidityDate { get; set; }

        [Required]
        [RegularExpression("^(Tervezet|Elküldve|Elfogadva|Elutasítva)$", ErrorMessage = "Nem létező státusz")]
        public string Status { get; set; }

        [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
        public string Subject { get; set; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        public string DetailedDescription { get; set; }

        [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount amount cannot be negative")]
        public decimal? DiscountAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Total amount cannot be negative")]
        public decimal? TotalAmount { get; set; }
    }

        public class PartnerDto
    {
        public int PartnerId { get; set; }
        public string Name { get; set; }
    }

}