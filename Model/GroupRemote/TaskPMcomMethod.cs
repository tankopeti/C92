using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    /// <summary>
    /// Kommunikációs módok (lookup table)
    /// Tábla: TaskPMcomMethod
    /// </summary>
    [Table("TaskPMcomMethod")]
    public class TaskPMcomMethod
    {
        /// <summary>
        /// Egyedi azonosító (PK, IDENTITY)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public short TaskPMcomMethodID { get; set; }

        /// <summary>
        /// Megnevezés magyarul (pl. "Mobiltelefon", "E-mail")
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("nev")]
        public string Nev { get; set; } = null!;

        /// <summary>
        /// Rövid angol/latin kód (pl. MOBILE, EMAIL) – opcionális, de általában használjuk API-khoz
        /// </summary>
        [MaxLength(20)]
        [Column("kod")]
        public string? Kod { get; set; }

        /// <summary>
        /// Részletes leírás (opcionális)
        /// </summary>
        [MaxLength(255)]
        [Column("leiras")]
        public string? Leiras { get; set; }

        /// <summary>
        /// Aktív-e a kommunikációs mód (1 = igen, 0 = nem)
        /// </summary>
        [Required]
        [Column("aktiv")]
        public bool? Aktiv { get; set; } = true;

        /// <summary>
        /// Sorrend a listákban (pl. legördülőben)
        /// </summary>
        [Required]
        [Column("sorrend")]
        public short? Sorrend { get; set; } = 0;

        /// <summary>
        /// Rekord létrehozásának időpontja
        /// </summary>
        [Required]
        [Column("letrehozva")]
        public DateTime? Letrehozva { get; set; } = DateTime.Now;

        /// <summary>
        /// Utolsó módosítás időpontja (NULL = még nem módosították)
        /// </summary>
        [Column("modositva")]
        public DateTime? Modositva { get; set; }

        // --------------------------------------------------------------------
        // Navigation property (ha van kapcsolódó tábla, pl. TaskPMcom)
        // --------------------------------------------------------------------
        // public virtual ICollection<TaskPMcom> TaskPMcoms { get; set; } = new List<TaskPMcom>();
    }
}