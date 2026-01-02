using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Cloud9_2.Models
{
    public class Document
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int? DocumentTypeId { get; set; }
        public DateTime? UploadDate { get; set; }
        public string UploadedBy { get; set; }
        public int? SiteId { get; set; }
        public Site? Site { get; set; }    
        public int? PartnerId { get; set; }
        [ForeignKey("PartnerId")]
        public Partner? Partner { get; set; }
        public DocumentType DocumentType { get; set; }
        public DocumentStatusEnum Status { get; set; }
        public ICollection<DocumentMetadata> DocumentMetadata { get; set; }
        public ICollection<DocumentLink> DocumentLinks { get; set; }
        public ICollection<DocumentStatusHistory> StatusHistory { get; set; }
public virtual ICollection<TaskDocumentLink> TaskDocuments { get; set; } = new List<TaskDocumentLink>();        
        // public int? EmployeeId { get; set; }
        // public Employee Employee { get; set; }
    }

    public class DocumentDto
    {
         [Key]
        public int DocumentId { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public int? DocumentTypeId { get; set; }
        public string? DocumentTypeName { get; set; }
        public DateTime? UploadDate { get; set; }
        public string? UploadedBy { get; set; }
        public int? SiteId { get; set; }
        public int? PartnerId { get; set; }
        public string? PartnerName { get; set; }
        // public int? EmployeeId { get; set; }
        public DocumentStatusEnum Status { get; set; }
        public List<DocumentLinkDto>? DocumentLinks { get; set; }
        public List<DocumentStatusHistoryDto>? StatusHistory { get; set; } = new List<DocumentStatusHistoryDto>();
        public static IDictionary<string, string> StatusDisplayNames { get; } = GetStatusDisplayNames();

        private static IDictionary<string, string> GetStatusDisplayNames()
        {
            return Enum.GetValues(typeof(DocumentStatusEnum))
                .Cast<DocumentStatusEnum>()
                .ToDictionary(
                    e => e.ToString(),
                    e => e switch
                    {
                        DocumentStatusEnum.Beérkezett => "Beérkezett",
                        DocumentStatusEnum.Függőben => "Függőben",
                        DocumentStatusEnum.Elfogadott => "Elfogadott",
                        DocumentStatusEnum.Lezárt => "Lezárt",
                        DocumentStatusEnum.Jóváhagyandó => "Jóváhagyandó",
                        _ => e.ToString()
                    });
        }
    }

    public class DocumentStatusHistoryDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public DocumentStatusEnum OldStatus { get; set; }
        public DocumentStatusEnum NewStatus { get; set; }
        public DateTime ChangeDate { get; set; }
        public string? ChangedBy { get; set; }
    }

    public class DocumentLinkDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string ModuleId { get; set; }
        public int RecordId { get; set; }
    }

    public class MetadataEntry
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }

    public class CreateDocumentDto
    {
        [Required]
        public string FileName { get; set; }
        [Required]
        public string FilePath { get; set; }
        public int? DocumentTypeId { get; set; }
        public int? SiteId { get; set; }
        // public int? EmployeeId { get; set; }
        public int? PartnerId { get; set; }
        [Required]
        public DocumentStatusEnum Status { get; set; }
        public List<MetadataEntry>? CustomMetadata { get; set; } = new List<MetadataEntry>();
    }

    public class DocumentModalViewModel
    {
        public DocumentDto? Document { get; set; }
        public CreateDocumentDto? CreateDocument { get; set; }
        public List<SelectListItem>? DocumentTypes { get; set; }
        public List<SelectListItem>? Partners { get; set; }
        public List<SelectListItem>? Sites { get; set; }
        public string? NextDocumentNumber { get; set; }
    }
    
}