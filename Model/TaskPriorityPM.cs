using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
public class TaskPriorityPM
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; }
    
    [StringLength(255)]
    public string Description { get; set; }
    
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string ColorCode { get; set; }
    public string Icon { get; set; }
    
    public ICollection<TaskPM> Tasks { get; set; }
}

}