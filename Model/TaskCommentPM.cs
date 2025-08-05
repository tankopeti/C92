using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class TaskCommentPM
    {
        public int Id { get; set; }
        
        public int TaskPMId { get; set; }
        public TaskPM TaskPM { get; set; }
        
        [Required]
        public string Comment { get; set; }
        
        public string CreatedById { get; set; }
        public ApplicationUser CreatedBy { get; set; }
        
        public DateTime CreatedDate { get; set; }
    }

}