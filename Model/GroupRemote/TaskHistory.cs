using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class TaskHistory
    {
        [Key]
        public int TaskHistoryId { get; set; }

        public int TaskPMId { get; set; }
        public TaskPM TaskPM { get; set; }

        public string? ModifiedById { get; set; }
        public ApplicationUser? ModifiedBy { get; set; }

        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        [StringLength(500, ErrorMessage = "Change description cannot exceed 500 characters")]
        public string? ChangeDescription { get; set; }
    }

    public class TaskHistoryDto
    {
        public int TaskHistoryId { get; set; }
        public int TaskPMId { get; set; }
        public string? ModifiedById { get; set; }
        public string? ModifiedByName { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string? ChangeDescription { get; set; }
    }
    
}