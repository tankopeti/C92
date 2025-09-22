using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class TaskAttachmentPM
    {
        public int TaskAttachmentPMId { get; set; }
        
        public int TaskPMId { get; set; }
        public TaskPM TaskPM { get; set; }
        
        [Required]
        [StringLength(255)]
        public string FileName { get; set; }
        
        [Required]
        public string FilePath { get; set; }
        
        public int? FileSize { get; set; }
        
        public string UploadedById { get; set; }
        public ApplicationUser UploadedBy { get; set; }
        
        public DateTime UploadDate { get; set; }
    }

}