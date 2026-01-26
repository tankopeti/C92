using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class GFO
    {
        [Key]
        public int GFOId { get; set; }
        public int GFOKod { get; set; }
        public string? GFOName { get; set; }
        public string? ChangeType { get; set; }
        public ICollection<Partner>? Partners { get; set; }

    }

    public class GFODto
    {
        [Key]
        public int GFOKod { get; set; }
        public string? GFOName { get; set; }
        public string? ChangeType { get; set; }
        public List<PartnerDto>? Partners { get; set; } = new List<PartnerDto>();

    }
}