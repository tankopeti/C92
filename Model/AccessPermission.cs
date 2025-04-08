using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class AccessPermission
    {
        [Key]
        public int AccessPermissionId { get; set; }

        [Required]
        public string RoleId { get; set; } // From AspNetRoles table

        [Required]
        public string PagePath { get; set; } // e.g., "/CRM/Partners"

        public bool CanViewPage { get; set; } // Permission to view the entire page

        public string? ColumnName { get; set; } // e.g., "Name", "Email" (null for page-level only)

        public bool CanViewColumn { get; set; } // Permission to view the specific column
    }
}