using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class TaskTypePM
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(255)]
        public string Description { get; set; }
        
        [StringLength(50)]
        public string Icon { get; set; }

        public bool IsActive { get; set; } = true;
        
        public ICollection<TaskPM> Tasks { get; set; }
    }
}