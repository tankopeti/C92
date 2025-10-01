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
        public int QuoteId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int VatTypeId { get; set; }

        [StringLength(200)]
        public string? ItemDescription { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Required]
        public decimal NetDiscountedPrice { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        public int? DiscountTypeId { get; set; }
        public DiscountType? DiscountType { get; set; }

        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? PartnerPrice { get; set; }
        public decimal? VolumePrice { get; set; }
        public decimal? ListPrice { get; set; }
        public VatTypeDto? VatType { get; set; }
        public int? VolumeThreshold { get; set; }
        public decimal? GrossPrice { get; set; }
        public int? VatRate { get; set; }
    
    }
    public class CreateQuoteItemDto
    {
        [Required]
        [Display(Name = "Tétel azonosító")]
        public int QuoteItemId { get; set; }

        [Required]
        [Display(Name = "Termék")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Mennyiség")]
        [Range(1, int.MaxValue, ErrorMessage = "A mennyiségnek pozitívnak kell lennie")]
        public decimal Quantity { get; set; }

        [Required]
        [Display(Name = "Listaár")]
        public decimal ListPrice { get; set; }

        [Required]
        [Display(Name = "Nettó kedvezményes ár")]
        public decimal NetDiscountedPrice { get; set; }

        [Required]
        [Display(Name = "Összesen")]
        public decimal TotalPrice { get; set; }

        [Required]
        [Display(Name = "ÁFA típus")]
        public int VatTypeId { get; set; }

        [Display(Name = "Kedvezmény típus")]
        public int? DiscountTypeId { get; set; }

        [Display(Name = "Kedvezmény összeg")]
        public decimal? DiscountAmount { get; set; }

        [Display(Name = "Partnerár")]
        public decimal? PartnerPrice { get; set; }

        [Display(Name = "Mennyiségi ár")]
        public decimal? VolumePrice { get; set; }
    }


    public class UpdateQuoteItemDto
    {
        [Required]
        [Display(Name = "Tétel azonosító")]
        public int QuoteItemId { get; set; }

        [Required]
        [Display(Name = "Termék")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Mennyiség")]
        [Range(1, int.MaxValue, ErrorMessage = "A mennyiségnek pozitívnak kell lennie")]
        public decimal Quantity { get; set; }

        [Required]
        [Display(Name = "Listaár")]
        public decimal ListPrice { get; set; }

        [Required]
        [Display(Name = "Nettó kedvezményes ár")]
        public decimal NetDiscountedPrice { get; set; }

        [Required]
        [Display(Name = "Összesen")]
        public decimal TotalPrice { get; set; }

        [Required]
        [Display(Name = "ÁFA típus")]
        public int VatTypeId { get; set; }

        [Display(Name = "Kedvezmény típus")]
        public int? DiscountTypeId { get; set; }

        [Display(Name = "Kedvezmény összeg")]
        public decimal? DiscountAmount { get; set; }

        [Display(Name = "Partnerár")]
        public decimal? PartnerPrice { get; set; }

        [Display(Name = "Mennyiségi ár")]
        public decimal? VolumePrice { get; set; }
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
        public VatTypeDto? VatType { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? PartnerPrice { get; set; }
        public int? VolumeThreshold { get; set; }
        public decimal? VolumePrice { get; set; }
        public decimal? ListPrice { get; set; }
        public int? DiscountTypeId { get; set; }
        public DiscountType? DiscountType { get; set; }
        }

        public enum DiscountType
        {
            NoDiscount = 1,
            ListPrice = 2,
            PartnerPrice = 3,
            VolumeDiscount = 4,
            CustomDiscountPercentage = 5,
            CustomDiscountAmount = 6
        }
}