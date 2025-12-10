using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class TaskResourceAssignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TaskResourceAssignmentId { get; set; }
        public int TaskPMId { get; set; }
        public TaskPM TaskPM { get; set; }

        public int ResourceId { get; set; }
        public Resource Resource { get; set; }
    }

}