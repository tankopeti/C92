using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    [Table("ResourceHistory")]
    public class ResourceHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ResourceHistoryId { get; set; }  // Matches SQL PK

        [Required]
        public int ResourceId { get; set; }

        [ForeignKey("ResourceId")]
        public Resource Resource { get; set; } = null!;

        [MaxLength(450)]
        public string? ModifiedById { get; set; }

        [ForeignKey("ModifiedById")]
        public ApplicationUser? ModifiedBy { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? ModifiedDate { get; set; }

        [MaxLength(500)]
        public string? ChangeDescription { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999.99)]
        public decimal? ServicePrice { get; set; }
    }

    public class ResourceHistoryDto
    {
        public int ResourceHistoryId { get; set; }
        public int ResourceId { get; set; }
        public string? ModifiedById { get; set; }
        public string? ModifiedByName { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ChangeDescription { get; set; }
        public decimal? ServicePrice { get; set; }
    }
}