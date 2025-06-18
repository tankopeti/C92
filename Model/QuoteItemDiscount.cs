using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public enum DiscountType
    {
        NoDiscount,
        CustomDiscountPercentage,
        CustomDiscountAmount,
        PartnerPrice,
        VolumeDiscount
    }

    public class QuoteItemDiscount
    {
        [Key]
        public int QuoteItemDiscountId { get; set; }

        [Required]
        public int QuoteItemId { get; set; }

        [ForeignKey("QuoteItemId")]
        public QuoteItem QuoteItem { get; set; }

        [Required]
        public DiscountType DiscountType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPercentage { get; set; } // For CustomDiscountPercentage

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountAmount { get; set; } // For CustomDiscountAmount

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BasePrice { get; set; } // Original price before discount

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PartnerPrice { get; set; } // For PartnerPrice

        public int? VolumeThreshold { get; set; } // For VolumeDiscount

        [Column(TypeName = "decimal(18,2)")]
        public decimal? VolumePrice { get; set; } // For VolumeDiscount
    }

}