using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class CustomerCommunication
    {
        [Key]
        public int CustomerCommunicationId { get; set; }

        [Required]
        public int CommunicationTypeId { get; set; }
        [ForeignKey("CommunicationTypeId")]
        public CommunicationType CommunicationType { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(100)]
        public string? Subject { get; set; }

        public string? Note { get; set; }

        public string? AgentId { get; set; } // Links to ASP.NET Identity UserId
        [ForeignKey("AgentId")]
        public ApplicationUser? Agent { get; set; }

        [Required]
        public int StatusId { get; set; }
        [ForeignKey("StatusId")]
        public CommunicationStatus Status { get; set; } = null!;

        [MaxLength(500)]
        public string? AttachmentPath { get; set; }

        [MaxLength(1000)]
        public string? Metadata { get; set; }

        public int? ContactId { get; set; }
        [ForeignKey("ContactId")]
        public Contact? Contact { get; set; }

        public int? PartnerId { get; set; }
        [ForeignKey("PartnerId")]
        public Partner? Partner { get; set; }

        public int? LeadId { get; set; }
        [ForeignKey("LeadId")]
        public Lead? Lead { get; set; }

        public int? QuoteId { get; set; }
        [ForeignKey("QuoteId")]
        public Quote? Quote { get; set; }

        public int? OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        public int? SiteId { get; set; }
        [ForeignKey("SiteId")]
        public Site? Site { get; set; }

        public List<CommunicationPost> Posts { get; set; } = new();
        public List<CommunicationResponsible> ResponsibleHistory { get; set; } = new();
    }
}