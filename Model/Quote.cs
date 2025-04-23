using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class Quote
    {
        [Key]
        public int QuoteId { get; set; }

        [StringLength(100)]
        [Display(Name = "Árajánlat száma")]
        public string? QuoteNumber { get; set; }

        [Display(Name = "Dátum")]
        [DataType(DataType.Date)]
        public DateTime? QuoteDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Leírás")]
        public string? Description { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Összesen")]
        public decimal? TotalAmount { get; set; }

        [StringLength(100)]
        [Display(Name = "Értékesítő")]
        public string? SalesPerson { get; set; }

        [Display(Name = "Érvényesség dátuma")]
        [DataType(DataType.Date)]
        public DateTime? ValidityDate { get; set; }

        [Display(Name = "Kedvezmény %")]
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Kedvezmény összeg")]
        public decimal? DiscountAmount { get; set; }

        [StringLength(100)]
        [Display(Name = "Cég neve")]
        public string? CompanyName { get; set; }

        [StringLength(200)]
        [Display(Name = "Tárgy")]
        public string? Subject { get; set; }

        [Display(Name = "Részletes leírás")]
        [DataType(DataType.MultilineText)]
        public string? DetailedDescription { get; set; }

        // Audit fields
        [StringLength(100)]
        [Display(Name = "Létrehozta")]
        public string? CreatedBy { get; set; } = "System";

        [Display(Name = "Létrehozás dátuma")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        [Display(Name = "Módosította")]
        public string? ModifiedBy { get; set; } = "System";

        [Display(Name = "Módosítás dátuma")]
        [DataType(DataType.DateTime)]
        public DateTime? ModifiedDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        [Display(Name = "Státusz")]
        public string? Status { get; set; } = "Draft";

        [Required]
        [Display(Name = "Partner")]
        public int PartnerId { get; set; }

        [ForeignKey("PartnerId")]
        public Partner? Partner { get; set; }


        [Display(Name = "Tételek")]
        public List<QuoteItem>? QuoteItems { get; set; } = new List<QuoteItem>();

    }
}