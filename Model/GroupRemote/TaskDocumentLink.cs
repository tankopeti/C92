using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
[Table("TaskDocumentLinks")]
    public class TaskDocumentLink
    {
        [Key]
        public int Id { get; set; }

        public int TaskId { get; set; }
        public TaskPM Task { get; set; } = null!;

        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        public DateTime LinkedDate { get; set; } = DateTime.UtcNow;
        public string? LinkedById { get; set; }
        public ApplicationUser? LinkedBy { get; set; }

        public string? Note { get; set; }
    }
    public class TaskDocumentDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public DateTime LinkedDate { get; set; }
        public string? LinkedByName { get; set; }
        public string? Note { get; set; }
    }


}