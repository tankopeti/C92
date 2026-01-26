using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class PartnerType
    {
        public int PartnerTypeId { get; set; }
        public string PartnerTypeName { get; set; }

        public ICollection<Partner>? Partners { get; set; }
    }
}