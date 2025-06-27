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

        [Required]
        [Display(Name = "Árajánlat azonosító")]
        public int QuoteId { get; set; }

        [Required]
        [Display(Name = "Termék azonosító")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "ÁFA típus")]
        public int VatTypeId { get; set; }

        [StringLength(200)]
        [Display(Name = "Tétel leírása")]
        public string? ItemDescription { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "A mennyiség pozitív kell legyen")]
        [Display(Name = "Mennyiség")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Az ár nem lehet negatív")]
        [Display(Name = "Nettó kedvezményes ár")]
        public decimal NetDiscountedPrice { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Az összesen nem lehet negatív")]
        [Display(Name = "Összesen")]
        public decimal TotalPrice { get; set; }

        [Range(1, 6, ErrorMessage = "A kedvezmény típusa 1 és 6 között kell legyen")]
        [Display(Name = "Kedvezmény típusa")]
        public DiscountType? DiscountType { get; set; }

        [Range(0, 100, ErrorMessage = "A kedvezmény százaléka 0 és 100 között kell legyen")]
        [Display(Name = "Kedvezmény %")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A kedvezmény összege nem lehet negatív")]
        [Display(Name = "Kedvezmény összeg")]
        public decimal? DiscountAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Az alapár nem lehet negatív")]
        [Display(Name = "Alapár")]
        public decimal? BasePrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A partner ár nem lehet negatív")]
        [Display(Name = "Partner ár")]
        public decimal? PartnerPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "A mennyiségi küszöb nem lehet negatív")]
        [Display(Name = "Mennyiségi küszöb")]
        public int? VolumeThreshold { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A mennyiségi ár nem lehet negatív")]
        [Display(Name = "Mennyiségi ár")]
        public decimal? VolumePrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "A listaár nem lehet negatív")]
        [Display(Name = "Listaár")]
        public decimal? ListPrice { get; set; }
    }
    public class CreateQuoteItemDto
    {
        public int ProductId { get; set; }
        public int VatTypeId { get; set; }
        public string? ItemDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal NetDiscountedPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DiscountType? DiscountType { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? PartnerPrice { get; set; }
        public int? VolumeThreshold { get; set; }
        public decimal? VolumePrice { get; set; }
        public decimal? ListPrice { get; set; }
    }

    public class UpdateQuoteItemDto
    {
        public int ProductId { get; set; }
        public int VatTypeId { get; set; }
        public string? ItemDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal NetDiscountedPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DiscountType? DiscountType { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? PartnerPrice { get; set; }
        public int? VolumeThreshold { get; set; }
        public decimal? VolumePrice { get; set; }
        public decimal? ListPrice { get; set; }
    }

    public class QuoteItemResponseDto
        {
        public int QuoteItemId { get; set; }
        public int QuoteId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal NetDiscountedPrice { get; set; }
        public string? ItemDescription { get; set; }
        public decimal TotalPrice { get; set; }
        public int VatTypeId { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? PartnerPrice { get; set; }
        public int? VolumeThreshold { get; set; }
        public decimal? VolumePrice { get; set; }
        public decimal? ListPrice { get; set; }
        public DiscountType? DiscountType { get; set; }
        }

}