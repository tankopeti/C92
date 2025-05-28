using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class CustomerCommunication
    {
        [Key]
        public int CustomerCommunicationId { get; set; }
        public int CommunicationTypeId { get; set; }
        public CommunicationType CommunicationType { get; set; }
        public DateTime Date { get; set; }
        [MaxLength(100)]
        public string? Subject { get; set; }
        public string? Note { get; set; }
        public string? AgentId { get; set; } // Changed to string for ASP.NET Identity UserId
        public ApplicationUser? Agent { get; set; } // Navigation to Identity user
        public int StatusId { get; set; }
        public CommunicationStatus Status { get; set; }
        [MaxLength(500)]
        public string? AttachmentPath { get; set; }
        [MaxLength(1000)]
        public string? Metadata { get; set; }
        public int? ContactId { get; set; }
        public Contact? Contact { get; set; }
        public int? PartnerId { get; set; }
        public Partner? Partner { get; set; }
        public int? LeadId { get; set; }
        public Lead? Lead { get; set; }
        public int? QuoteId { get; set; }
        public Quote? Quote { get; set; }
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
        public List<CommunicationPost> Posts { get; set; }
        public List<CommunicationResponsible> ResponsibleHistory { get; set; }
    }
}