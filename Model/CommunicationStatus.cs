using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class CommunicationStatus
    {
        [Key]
        public int StatusId { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; } // e.g., "Open", "InProgress", "Escalated"
        [MaxLength(200)]
        public string? Description { get; set; }

        public ICollection<CustomerCommunication> CustomerCommunications { get; set; } = new List<CustomerCommunication>();
    }
}