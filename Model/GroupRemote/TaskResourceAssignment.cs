using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class TaskResourceAssignment
    {
        [Key]
        public int TaskResourceAssignmentId { get; set; }
        public int TaskPMId { get; set; }
        public TaskPM TaskPM { get; set; }

        public int ResourceId { get; set; }
        public Resource Resource { get; set; }
    }

}