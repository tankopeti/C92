using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class QuoteHistory
    {
        public int QuoteHistoryId { get; set; }
        public int QuoteId { get; set; }
        public int? QuoteItemId { get; set; }
        public string Action { get; set; }
        public string? FieldName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string? Comment { get; set; }

        // Navigation properties
        public Quote Quote { get; set; }
        public QuoteItem? QuoteItem { get; set; }
    }
}