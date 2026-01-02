using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        public string EntityType { get; set; } = null!; // pl. "Partner", "TaskPM"

        public int EntityId { get; set; }

        [Required]
        public string Action { get; set; } = null!; // "Created", "Updated", "Deleted"

        [Required]
        public string ChangedById { get; set; } = null!;

        public string? ChangedByName { get; set; }

        public DateTime ChangedAt { get; set; }

        [Required]
        public string Changes { get; set; } = null!; // pl. "Név: ABC Kft → XYZ Kft; Adószám: 123 → 456"
    }

    public class AuditLogDto
    {
        public string Action { get; set; } = null!;
        public string? ChangedByName { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Changes { get; set; } = null!;
    }
}