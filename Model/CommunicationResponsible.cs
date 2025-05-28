using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class CommunicationResponsible
    {
        public int CommunicationResponsibleId { get; set; }
        public int CustomerCommunicationId { get; set; }
        public CustomerCommunication CustomerCommunication { get; set; }
        public int? ResponsibleId { get; set; }
        public Contact? Responsible { get; set; }
        public int? AssignedById { get; set; }
        public Contact? AssignedBy { get; set; }
        public DateTime? AssignedAt { get; set; }
    }
}