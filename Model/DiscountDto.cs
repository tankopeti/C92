using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
public class DiscountDto
    {
        [Required(ErrorMessage = "DiscountId is required")]
        public int DiscountId { get; set; }

        [Required(ErrorMessage = "ItemId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ItemId must be a positive number")]
        public int ItemId { get; set; } // QuoteItemId or OrderItemId

        [Required(ErrorMessage = "DiscountType is required")]
        public string DiscountType { get; set; } // CustomDiscountPercentage, CustomDiscountAmount, PartnerPrice, VolumeDiscount

        [Range(0, 100, ErrorMessage = "DiscountPercentage must be between 0 and 100")]
        public decimal? DiscountPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "DiscountAmount must be non-negative")]
        public decimal? DiscountAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "BasePrice must be non-negative")]
        public decimal? BasePrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "PartnerPrice must be non-negative")]
        public decimal? PartnerPrice { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "VolumeThreshold must be a positive number")]
        public int? VolumeThreshold { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "VolumePrice must be non-negative")]
        public decimal? VolumePrice { get; set; }
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