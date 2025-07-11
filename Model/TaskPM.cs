using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
public class TaskPM
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Title { get; set; }
    
    public string? Description { get; set; }

    public bool? IsActive { get; set; } = true;
    
    public int? TaskTypePMId { get; set; }
    public TaskTypePM TaskTypePM { get; set; }
    
    public int? ProjectId { get; set; }
    public ProjectPM Project { get; set; }
    
    public int? TaskStatusPMId { get; set; }
    public TaskStatusPM TaskStatusPM { get; set; }
    
    public int? TaskPriorityPMId { get; set; }
    public TaskPriorityPM TaskPriorityPM { get; set; }
    
    [Display(Name = "Due Date")]
    public DateTime? DueDate { get; set; }
    
    [Display(Name = "Estimated Hours")]
    public decimal? EstimatedHours { get; set; }
    
    [Display(Name = "Actual Hours")]
    public decimal? ActualHours { get; set; }
    
    public string? CreatedById { get; set; }
    public ApplicationUser CreatedBy { get; set; }
    
    public DateTime? CreatedDate { get; set; }
    
    public string? AssignedToId { get; set; }
    public ApplicationUser AssignedTo { get; set; }

    public int? ProjectPMId { get; set; } 
    
    public virtual ProjectPM ProjectPM { get; set; }
    
    public DateTime? CompletedDate { get; set; }
    
    public ICollection<TaskCommentPM> Comments { get; set; }
    public ICollection<TaskAttachmentPM> Attachments { get; set; }
}


}