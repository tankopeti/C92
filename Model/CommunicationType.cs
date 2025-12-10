using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    [Table("CommunicationTypes")]
    public class CommunicationType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CommunicationTypeId { get; set; }

        /// <summary>
        /// Magyar név (pl. "Mobiltelefon", "E-mail")
        /// </summary>
        [Required]
        [Column("Name", TypeName = "NVARCHAR(50)")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Angol/műszaki név vagy kód (pl. "Mobile", "Email") – egyedi!
        /// </summary>
        [Column("method_name", TypeName = "NVARCHAR(50)")]
        [StringLength(50)]
        public string? MethodName { get; set; }

        /// <summary>
        /// Részletes leírás
        /// </summary>
        [Column("description", TypeName = "NVARCHAR(MAX)")]
        public string? Description { get; set; }

        /// <summary>
        /// Aktív-e a típus
        /// </summary>
        [Column("is_active")]
        public bool? IsActive { get; set; } = true;

        /// <summary>
        /// Sorrend megjelenítéshez
        /// </summary>
        [Column("sort_order")]
        public short? SortOrder { get; set; } = 0;

        /// <summary>
        /// Létrehozás időpontja
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Utolsó módosítás időpontja
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties (ha használod)
        public virtual ICollection<CustomerCommunication> CustomerCommunications { get; set; } 
            = new List<CustomerCommunication>();

        public virtual ICollection<TaskPM> TaskPMs { get; set; } 
            = new List<TaskPM>();
    }
}