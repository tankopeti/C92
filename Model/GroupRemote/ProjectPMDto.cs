using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
public class ProjectStatusPMDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int DisplayOrder { get; set; }
    public string ColorCode { get; set; }
}

// ProjectPMDto.cs
public class ProjectPMDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Budget { get; set; }
    public ProjectStatusPMDto Status { get; set; }
    
    // public UserDto CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsActive { get; set; }
    
    // For dropdowns/lists
    public string StatusName => Status?.Name;
    public string StatusColor => Status?.ColorCode;
}

// ProjectPMCreateDto.cs
public class ProjectPMCreateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    public string Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Budget { get; set; }
    public int ProjectStatusPMId { get; set; } = 1; // Default to Planning
}

// ProjectPMUpdateDto.cs
public class ProjectPMUpdateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    public string Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Budget { get; set; }
    public int ProjectStatusPMId { get; set; }
    public bool IsActive { get; set; }
}


}