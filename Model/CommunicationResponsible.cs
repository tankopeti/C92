using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class CommunicationResponsible
    {
        [Key]
        public int CommunicationResponsibleId { get; set; }

        [Required]
        public int CustomerCommunicationId { get; set; }
        [ForeignKey("CustomerCommunicationId")]
        public CustomerCommunication CustomerCommunication { get; set; } = null!;

        [Required]
        public string ResponsibleId { get; set; } = null!;
        [ForeignKey("ResponsibleId")]
        public ApplicationUser Responsible { get; set; } = null!;

        [Required]
        public string AssignedById { get; set; } = null!;
        [ForeignKey("AssignedById")]
        public ApplicationUser AssignedBy { get; set; } = null!;

        [Required]
        public DateTime AssignedAt { get; set; }
    }
}