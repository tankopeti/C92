using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
public class ProjectStatusPM
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; }
    
    [StringLength(255)]
    public string Description { get; set; }
    
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string ColorCode { get; set; } = "#cccccc";
    public bool IsSystem { get; set; } = false;
    
    public ICollection<ProjectPM> Projects { get; set; }
}


}