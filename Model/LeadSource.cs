using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class LeadSource
    {
        public int LeadSourceId { get; set; }
        public string LeadSourceName { get; set; }

        // Foreign key to Partner
        public int PartnerId { get; set; }
        public Partner Partner { get; set; }
    }
}