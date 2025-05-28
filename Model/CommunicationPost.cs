using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class CommunicationPost
    {
        public int CommunicationPostId { get; set; }
        public int CustomerCommunicationId { get; set; }
        public CustomerCommunication CustomerCommunication { get; set; }
        [Required]
        public string? Content { get; set; }
        public int? CreatedById { get; set; }
        public Contact? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}