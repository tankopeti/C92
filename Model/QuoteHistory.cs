namespace Cloud9_2.Models
{
    public class QuoteHistory
    {
        public int QuoteHistoryId { get; set; }
        public int QuoteId { get; set; }
        public string? ChangeType { get; set; }
        public DateTime ChangeDate { get; set; }
        public string? ChangedBy { get; set; }
        public string? QuoteNumber { get; set; }
        public DateTime? QuoteDate { get; set; }
        public int? PartnerId { get; set; }
        public string? Description { get; set; }
        public decimal? TotalAmount { get; set; }
        public Quote? Quote { get; set; } // Added navigation property
    }
}