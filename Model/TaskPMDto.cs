using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
// TaskStatusPMDto.cs
public class TaskStatusPMDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ColorCode { get; set; }
}

// TaskPriorityPMDto.cs
public class TaskPriorityPMDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ColorCode { get; set; }
    public string Icon { get; set; }
}

// TaskTypePMDto.cs
public class TaskTypePMDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
}

// TaskPMDto.cs
public class TaskPMDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public TaskTypePMDto TaskType { get; set; }
    public ProjectPMDto Project { get; set; }
    public TaskStatusPMDto Status { get; set; }
    public TaskPriorityPMDto Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public string? CreatedBy { get; set; } = "System";
    public DateTime CreatedDate { get; set; }
    public string? AssignedTo { get; set; } = "System";
    public DateTime? CompletedDate { get; set; }
    public bool IsActive { get; set; }
    
    // For UI display
    public string StatusName => Status?.Name;
    public string PriorityName => Priority?.Name;
    public string ProjectName => Project?.Name;
}

// TaskPMCreateDto.cs
public class TaskPMCreateDto
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; }
    
    public string Description { get; set; }
    public int TaskTypePMId { get; set; }
    public int? ProjectPMId { get; set; }
    public int TaskStatusPMId { get; set; } = 1; // Default to To Do
    public int TaskPriorityPMId { get; set; } = 2; // Default to Medium
    public DateTime? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public string AssignedToId { get; set; }
}

// TaskPMUpdateDto.cs
public class TaskPMUpdateDto
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; }
    
    public string Description { get; set; }
    public int TaskTypePMId { get; set; }
    public int? ProjectPMId { get; set; }
    public int TaskStatusPMId { get; set; }
    public int TaskPriorityPMId { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public string AssignedToId { get; set; }
    public bool IsActive { get; set; }
}

}