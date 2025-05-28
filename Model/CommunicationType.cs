using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class CommunicationType
    {
        [Key]
    public int CommunicationTypeId { get; set; }
    public string Name { get; set; } // e.g. "Email", "Phone", "Meeting"
    public ICollection<CustomerCommunication> CustomerCommunications { get; set; }


    }
}