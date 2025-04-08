using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class PartnerType
    {
        public int PartnerTypeId { get; set; }
        public string PartnerTypeName { get; set; }

        // Foreign key to Partner
        public int PartnerId { get; set; }
        public Partner Partner { get; set; }
    }
}