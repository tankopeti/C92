using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class CommunicationPost
    {
        [Key]
        public int CommunicationPostId { get; set; }

        [Required]
        public int CustomerCommunicationId { get; set; }
        [ForeignKey("CustomerCommunicationId")]
        public CustomerCommunication CustomerCommunication { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        [Required]
        public string CreatedById { get; set; } = null!;
        [ForeignKey("CreatedById")]
        public ApplicationUser CreatedBy { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}