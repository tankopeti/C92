using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
public class ProjectPM
{
    public int ProjectPMId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    [Display(Name = "Start Date")]
    public DateTime? StartDate { get; set; }
    
    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }
    
    public decimal? Budget { get; set; }
    
    public int ProjectStatusPMId { get; set; }
    public ProjectStatusPM ProjectStatusPM { get; set; }

    
    public string CreatedById { get; set; }
    public ApplicationUser CreatedBy { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    public bool IsActive { get; set; } = true;

    public virtual ICollection<TaskPM> Tasks { get; set; } = new List<TaskPM>();
}

}