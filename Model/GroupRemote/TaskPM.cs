using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class TaskPM
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int? TaskTypePMId { get; set; }
        public TaskTypePM? TaskTypePM { get; set; }

        // public int? ProjectId { get; set; }
        // public Project? Project { get; set; }

        public int? TaskStatusPMId { get; set; }
        public TaskStatusPM? TaskStatusPM { get; set; }

        public int? TaskPriorityPMId { get; set; }
        public TaskPriorityPM? TaskPriorityPM { get; set; }

        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Beütemeézés dátuma")]
        public DateTime? ScheduledDate { get; set; }

        [Display(Name = "Estimated Hours")]
        [Range(0, 999.99, ErrorMessage = "Estimated hours must be between 0 and 999.99")]
        public decimal? EstimatedHours { get; set; }

        [Display(Name = "Actual Hours")]
        [Range(0, 999.99, ErrorMessage = "Actual hours must be between 0 and 999.99")]
        public decimal? ActualHours { get; set; }

        public string? CreatedById { get; set; }

        [Display(Name = "Kommunikáció módja")]
        public string? CommunicationDescription { get; set; }
        public ApplicationUser? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

        public string? AssignedToId { get; set; }
        public ApplicationUser? AssignedTo { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "Completed Date")]
        public DateTime? CompletedDate { get; set; }

        // public int? ProjectPMId { get; set; }
        // public ProjectPM? ProjectPM { get; set; }

        // New fields
        public int? PartnerId { get; set; }
        public Partner? Partner { get; set; }

        public int? SiteId { get; set; }
        public Site? Site { get; set; }

        public int? ContactId { get; set; }
        public Contact? Contact { get; set; }

        public int? QuoteId { get; set; }
        public Quote? Quote { get; set; }

        public int? CommunicationTypeId { get; set; }
        public CommunicationType? CommunicationType { get; set; }

        public int? OrderId { get; set; }
        public Order? Order { get; set; }
        // public int? ProjectId { get; set; }
        // public ProjectPM? Project { get; set; }

        public int? CustomerCommunicationId { get; set; }
        public CustomerCommunication? CustomerCommunication { get; set; }

        // Navigation properties
        public ICollection<TaskCommentPM> Comments { get; set; } = new List<TaskCommentPM>();
        public ICollection<TaskAttachmentPM> Attachments { get; set; } = new List<TaskAttachmentPM>();
        public ICollection<TaskResourceAssignment> TaskResourceAssignments { get; set; } = new List<TaskResourceAssignment>();
        public ICollection<TaskEmployeeAssignment> TaskEmployeeAssignments { get; set; } = new List<TaskEmployeeAssignment>();
        public ICollection<TaskHistory> TaskHistories { get; set; } = new List<TaskHistory>();
        public virtual ICollection<TaskDocumentLink> TaskDocuments { get; set; } = new List<TaskDocumentLink>();
    }

    public class TaskPMDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int? TaskTypePMId { get; set; }
        public string? TaskTypePMName { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public int? TaskStatusPMId { get; set; }
        public string? TaskStatusPMName { get; set; }
        public string? ColorCode { get; set; }
        public int? TaskPriorityPMId { get; set; }
        public string? TaskPriorityPMName { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public decimal? EstimatedHours { get; set; }
        public decimal? ActualHours { get; set; }
        public string? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        // public int? ProjectPMId { get; set; }
        // public string? ProjectPMName { get; set; }
        public int? PartnerId { get; set; }
        public string? PartnerName { get; set; }
        public int? SiteId { get; set; }
        public string? SiteName { get; set; }
        public string? City { get; set; }
        public int? ContactId { get; set; }
        public string? ContactName { get; set; }
        public int? QuoteId { get; set; }
        public string? QuoteNumber { get; set; }
        public int? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public int? CustomerCommunicationId { get; set; }
        public string? CustomerCommunicationSubject { get; set; }
        public int? CommunicationTypeId { get; set; }
        public string? CommunicationTypeName { get; set; }
        public string? CommunicationDescription { get; set; }
        public List<int> ResourceIds { get; set; } = new List<int>();
        public List<int> EmployeeIds { get; set; } = new List<int>();
        public List<TaskHistoryDto> TaskHistories { get; set; } = new List<TaskHistoryDto>();
        public virtual ICollection<TaskDocumentLink> TaskDocuments { get; set; } = new List<TaskDocumentLink>();
        public List<TaskDocumentDto> Attachments { get; set; } = new();

        public string? AssignedToPhone { get; set; }
    }

    public class TaskCreateDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; }

        public string? Description { get; set; }
        public string? CommunicationDescription { get; set; }

        public bool IsActive { get; set; } = true;

        public int? TaskTypePMId { get; set; }

        public int? ProjectId { get; set; }

        public int? TaskStatusPMId { get; set; }

        public int? TaskPriorityPMId { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? ScheduledDate { get; set; }

        [Range(0, 999.99, ErrorMessage = "Estimated hours must be between 0 and 999.99")]
        public decimal? EstimatedHours { get; set; }

        [Range(0, 999.99, ErrorMessage = "Actual hours must be between 0 and 999.99")]
        public decimal? ActualHours { get; set; }

        public string? AssignedToId { get; set; }

        // public int? ProjectPMId { get; set; }

        public int? PartnerId { get; set; }

        public int? SiteId { get; set; }

        public int? ContactId { get; set; }

        public int? QuoteId { get; set; }

        public int? OrderId { get; set; }

        public int? CustomerCommunicationId { get; set; }
        public int? CommunicationTypeId { get; set; }


        public List<int> ResourceIds { get; set; } = new List<int>();

        public List<int> EmployeeIds { get; set; } = new List<int>();
    }

    public class TaskUpdateDto
    {
        [Required(ErrorMessage = "Task ID is required")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; }

        public string? Description { get; set; }
        public string? CommunicationDescription { get; set; }

        public bool IsActive { get; set; }

        public int? TaskTypePMId { get; set; }

        public int? ProjectId { get; set; }

        public int? TaskStatusPMId { get; set; }

        public int? TaskPriorityPMId { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? ScheduledDate { get; set; }

        [Range(0, 999.99, ErrorMessage = "Estimated hours must be between 0 and 999.99")]
        public decimal? EstimatedHours { get; set; }

        [Range(0, 999.99, ErrorMessage = "Actual hours must be between 0 and 999.99")]
        public decimal? ActualHours { get; set; }

        public string? AssignedToId { get; set; }

        // public int? ProjectPMId { get; set; }

        public int? PartnerId { get; set; }

        public int? SiteId { get; set; }

        public int? ContactId { get; set; }

        public int? QuoteId { get; set; }

        public int? OrderId { get; set; }

        public int? CustomerCommunicationId { get; set; }
        public int? CommunicationTypeId { get; set; }

        public List<int>? ResourceIds { get; set; }

        public List<int>? EmployeeIds { get; set; }
    }
}