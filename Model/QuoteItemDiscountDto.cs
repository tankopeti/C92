using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{

    public class QuoteItemDiscountDto
    {
        public int QuoteItemDiscountId { get; set; }
        public int QuoteItemId { get; set; }
        [Required(ErrorMessage = "DiscountType is required")]
        public DiscountType DiscountType { get; set; }
        [Range(0, 100, ErrorMessage = "DiscountPercentage must be between 0 and 100")]
        public decimal? DiscountPercentage { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "DiscountAmount must be non-negative")]
        public decimal? DiscountAmount { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "BasePrice must be non-negative")]
        public decimal? BasePrice { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "PartnerPrice must be non-negative")]
        public decimal? PartnerPrice { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "VolumeThreshold must be non-negative")]
        public int? VolumeThreshold { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "VolumePrice must be non-negative")]
        public decimal? VolumePrice { get; set; }
    }
}