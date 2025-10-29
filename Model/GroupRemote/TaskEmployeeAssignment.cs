using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class TaskEmployeeAssignment
    {
        [Key]
        public int TaskEmployeeAssignmentId { get; set; }
        public int TaskPMId { get; set; }
        public TaskPM TaskPM { get; set; }

        public int EmployeeId { get; set; }
        public Employees Employee { get; set; }
    }

}