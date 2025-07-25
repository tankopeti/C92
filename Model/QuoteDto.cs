using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class QuoteDto
    {
        public int QuoteId { get; set; }

        [StringLength(100)]
        [Display(Name = "Árajánlat száma")]
        public string? QuoteNumber { get; set; }

        [Required(ErrorMessage = "A partner azonosító kötelező")]
        [Range(1, int.MaxValue, ErrorMessage = "A partner azonosító pozitív szám kell legyen")]
        [Display(Name = "Partner")]
        public int PartnerId { get; set; }

        [Required(ErrorMessage = "A pénznem azonosító kötelező")]
        [Display(Name = "Pénznem")]
        public int CurrencyId { get; set; }

        [Display(Name = "Pénznem")]
        public CurrencyDto? Currency { get; set; }

        [Display(Name = "Partner")]
        public PartnerDto? Partner { get; set; }

        [Display(Name = "Dátum")]
        [DataType(DataType.Date)]
        public DateTime? QuoteDate { get; set; }

        [StringLength(50)]
        [Display(Name = "Státusz")]
        public string? Status { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Összesen")]
        public decimal? TotalAmount { get; set; }

        [StringLength(100)]
        [Display(Name = "Értékesítő")]
        public string? SalesPerson { get; set; }

        [Display(Name = "Érvényesség dátuma")]
        [DataType(DataType.Date)]
        public DateTime? ValidityDate { get; set; }

        [StringLength(200)]
        [Display(Name = "Tárgy")]
        public string? Subject { get; set; }

        [StringLength(500)]
        [Display(Name = "Leírás")]
        public string? Description { get; set; }

        [Display(Name = "Részletes leírás")]
        [DataType(DataType.MultilineText)]
        public string? DetailedDescription { get; set; }

        [Range(0, 100, ErrorMessage = "A kedvezmény százaléka 0 és 100 között kell legyen")]
        [Display(Name = "Kedvezmény %")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A kedvezmény összege nem lehet negatív")]
        [DataType(DataType.Currency)]
        [Display(Name = "Kedvezmény összeg")]
        public decimal? QuoteDiscountAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A tételek kedvezménye nem lehet negatív")]
        [DataType(DataType.Currency)]
        [Display(Name = "Tételek kedvezménye")]
        public decimal? TotalItemDiscounts { get; set; }

        [StringLength(100)]
        [Display(Name = "Cég neve")]
        public string? CompanyName { get; set; }

        [StringLength(100)]
        [Display(Name = "Létrehozta")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Létrehozás dátuma")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Módosította")]
        public string? ModifiedBy { get; set; }

        [Display(Name = "Módosítás dátuma")]
        [DataType(DataType.DateTime)]
        public DateTime? ModifiedDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Referenciaszám")]
        public string? ReferenceNumber { get; set; }

        [Display(Name = "Tételek")]
        public List<QuoteItemDto> Items { get; set; } = new List<QuoteItemDto>();
    }

    public class CreateQuoteDto
    {
        [StringLength(100)]
        [Display(Name = "Árajánlat száma")]
        public string? QuoteNumber { get; set; }

        [Required(ErrorMessage = "A partner azonosító kötelező")]
        [Range(1, int.MaxValue, ErrorMessage = "A partner azonosító pozitív szám kell legyen")]
        [Display(Name = "Partner")]
        public int PartnerId { get; set; }

        [Required(ErrorMessage = "A pénznem azonosító kötelező")]
        [Display(Name = "Pénznem")]
        public int CurrencyId { get; set; }

        [Display(Name = "Dátum")]
        [DataType(DataType.Date)]
        public DateTime? QuoteDate { get; set; }

        [Required(ErrorMessage = "A státusz megadása kötelező")]
        [StringLength(50)]
        [Display(Name = "Státusz")]
        public string Status { get; set; } = "Draft";

        [Range(0, double.MaxValue, ErrorMessage = "Az összeg nem lehet negatív")]
        [DataType(DataType.Currency)]
        [Display(Name = "Összesen")]
        public decimal? TotalAmount { get; set; }

        [StringLength(100)]
        [Display(Name = "Értékesítő")]
        public string? SalesPerson { get; set; }

        [Display(Name = "Érvényesség dátuma")]
        [DataType(DataType.Date)]
        public DateTime? ValidityDate { get; set; }

        [Required(ErrorMessage = "A tárgy megadása kötelező")]
        [StringLength(200)]
        [Display(Name = "Tárgy")]
        public string Subject { get; set; }

        [StringLength(500)]
        [Display(Name = "Leírás")]
        public string? Description { get; set; }

        [StringLength(1000)]
        [Display(Name = "Részletes leírás")]
        public string? DetailedDescription { get; set; }

        [Range(0, 100, ErrorMessage = "A kedvezmény százaléka 0 és 100 között kell legyen")]
        [Display(Name = "Kedvezmény %")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A kedvezmény összege nem lehet negatív")]
        [DataType(DataType.Currency)]
        [Display(Name = "Kedvezmény összeg")]
        public decimal? DiscountAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A tételek kedvezménye nem lehet negatív")]
        [DataType(DataType.Currency)]
        [Display(Name = "Tételek kedvezménye")]
        public decimal? TotalItemDiscounts { get; set; }

        [StringLength(100)]
        [Display(Name = "Cég neve")]
        public string? CompanyName { get; set; }

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

        [StringLength(100)]
        [Display(Name = "Referenciaszám")]
        public string? ReferenceNumber { get; set; }

        [Display(Name = "Tételek")]
        public List<QuoteItemDto> Items { get; set; } = new List<QuoteItemDto>();
    }

    public class UpdateQuoteDto
    {
        [Required(ErrorMessage = "Az árajánlat száma kötelező")]
        [StringLength(100, ErrorMessage = "Az árajánlat száma maximum 100 karakter lehet")]
        [Display(Name = "Árajánlat száma")]
        public string? QuoteNumber { get; set; }

        [Required(ErrorMessage = "A partner azonosító kötelező")]
        [Range(1, int.MaxValue, ErrorMessage = "A partner azonosító pozitív szám kell legyen")]
        [Display(Name = "Partner")]
        public int PartnerId { get; set; }

        [Required(ErrorMessage = "A pénznem azonosító kötelező")]
        [Display(Name = "Pénznem")]
        public int CurrencyId { get; set; }

        [Required(ErrorMessage = "Az árajánlat dátuma kötelező")]
        [Display(Name = "Dátum")]
        [DataType(DataType.Date)]
        public DateTime? QuoteDate { get; set; }

        [Required(ErrorMessage = "A státusz kötelező")]
        [StringLength(50, ErrorMessage = "A státusz maximum 50 karakter lehet")]
        [Display(Name = "Státusz")]
        public string Status { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Az összeg nem lehet negatív")]
        [DataType(DataType.Currency)]
        [Display(Name = "Összesen")]
        public decimal? TotalAmount { get; set; }

        [StringLength(100, ErrorMessage = "Az értékesítő neve maximum 100 karakter lehet")]
        [Display(Name = "Értékesítő")]
        public string? SalesPerson { get; set; }

        [Display(Name = "Érvényesség dátuma")]
        [DataType(DataType.Date)]
        public DateTime? ValidityDate { get; set; }

        [Required(ErrorMessage = "A tárgy kötelező")]
        [StringLength(200, ErrorMessage = "A tárgy maximum 200 karakter lehet")]
        [Display(Name = "Tárgy")]
        public string Subject { get; set; }

        [StringLength(500, ErrorMessage = "A leírás maximum 500 karakter lehet")]
        [Display(Name = "Leírás")]
        public string? Description { get; set; }

        [StringLength(1000, ErrorMessage = "A részletes leírás maximum 1000 karakter lehet")]
        [Display(Name = "Részletes leírás")]
        public string? DetailedDescription { get; set; }

        [Range(0, 100, ErrorMessage = "A kedvezmény százaléka 0 és 100 között kell legyen")]
        [Display(Name = "Kedvezmény %")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A kedvezmény összege nem lehet negatív")]
        [DataType(DataType.Currency)]
        [Display(Name = "Kedvezmény összeg")]
        public decimal? DiscountAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A tételek kedvezménye nem lehet negatív")]
        [DataType(DataType.Currency)]
        [Display(Name = "Tételek kedvezménye")]
        public decimal? TotalItemDiscounts { get; set; }

        [StringLength(100)]
        [Display(Name = "Cég neve")]
        public string? CompanyName { get; set; }

        [StringLength(100)]
        [Display(Name = "Referenciaszám")]
        public string? ReferenceNumber { get; set; }
    }

    public class UpdateQuoteResponseDto
    {
        public int QuoteId { get; set; }

        [StringLength(100)]
        [Display(Name = "Árajánlat száma")]
        public string? QuoteNumber { get; set; }

        [Required]
        [Display(Name = "Partner")]
        public int PartnerId { get; set; }

        [Required]
        [Display(Name = "Pénznem")]
        public int CurrencyId { get; set; }

        [Display(Name = "Dátum")]
        [DataType(DataType.Date)]
        public DateTime? QuoteDate { get; set; }

        [StringLength(50)]
        [Display(Name = "Státusz")]
        public string? Status { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Összesen")]
        public decimal? TotalAmount { get; set; }

        [StringLength(100)]
        [Display(Name = "Értékesítő")]
        public string? SalesPerson { get; set; }

        [Display(Name = "Érvényesség dátuma")]
        [DataType(DataType.Date)]
        public DateTime? ValidityDate { get; set; }

        [StringLength(200)]
        [Display(Name = "Tárgy")]
        public string? Subject { get; set; }

        [StringLength(500)]
        [Display(Name = "Leírás")]
        public string? Description { get; set; }

        [StringLength(1000)]
        [Display(Name = "Részletes leírás")]
        public string? DetailedDescription { get; set; }

        [Range(0, 100)]
        [Display(Name = "Kedvezmény %")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue)]
        [DataType(DataType.Currency)]
        [Display(Name = "Kedvezmény összeg")]
        public decimal? QuoteDiscountAmount { get; set; }

        [Range(0, double.MaxValue)]
        [DataType(DataType.Currency)]
        [Display(Name = "Tételek kedvezménye")]
        public decimal? TotalItemDiscounts { get; set; }

        [StringLength(100)]
        [Display(Name = "Cég neve")]
        public string? CompanyName { get; set; }
    }

    public class ConvertQuoteToOrderDto
    {
        [Required(ErrorMessage = "A pénznem azonosító kötelező")]
        [Display(Name = "Pénznem")]
        public int CurrencyId { get; set; }

        [Display(Name = "Telephely azonosító")]
        public int? SiteId { get; set; }

        [StringLength(100)]
        [Display(Name = "Fizetési feltételek")]
        public string? PaymentTerms { get; set; }

        [StringLength(100)]
        [Display(Name = "Szállítási mód")]
        public string? ShippingMethod { get; set; }

        [StringLength(50)]
        [Display(Name = "Megrendelés típusa")]
        public string? OrderType { get; set; }
    }
}