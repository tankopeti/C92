using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [StringLength(100)]
        [Display(Name = "Rendelésszám")]
        public string? OrderNumber { get; set; }

        [Display(Name = "Rendelés dátuma")]
        [DataType(DataType.Date)]
        public DateTime? OrderDate { get; set; }

        [Display(Name = "Határidő")]
        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        [StringLength(500)]
        [Display(Name = "Leírás")]
        public string? Description { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Összesen")]
        public decimal? TotalAmount { get; set; }

        [StringLength(100)]
        [Display(Name = "Értékesítő")]
        public string? SalesPerson { get; set; }

        [Display(Name = "Szállítási dátum")]
        [DataType(DataType.Date)]
        public DateTime? DeliveryDate { get; set; }

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
        public string? Status { get; set; } = "Pending";

        [Required]
        [Display(Name = "Partner")]
        public int PartnerId { get; set; }

        [ForeignKey("PartnerId")]
        public Partner? Partner { get; set; }

        [Display(Name = "Partner telephely")]
        public int? SiteId { get; set; }

        [ForeignKey("SiteId")]
        public Site? Site { get; set; }

        [Required]
        [Display(Name = "Pénznem")]
        public int CurrencyId { get; set; }

        [ForeignKey("CurrencyId")]
        public Currency? Currency { get; set; }

        [StringLength(100)]
        [Display(Name = "Fizetési feltételek")]
        public string? PaymentTerms { get; set; }

        [StringLength(100)]
        [Display(Name = "Szállítási mód")]
        public string? ShippingMethod { get; set; }

        [StringLength(50)]
        [Display(Name = "Rendelés típusa")]
        public string? OrderType { get; set; }

        [Display(Name = "Tételek")]
        public List<OrderItem>? OrderItems { get; set; } = new List<OrderItem>();

        [StringLength(100)]
        [Display(Name = "Referenciaszám")]
        public string? ReferenceNumber { get; set; }

        [Display(Name = "Árajánlat azonosító")]
        public int? QuoteId { get; set; }

        [ForeignKey("QuoteId")]
        public Quote? Quote { get; set; }
        public ICollection<CustomerCommunication>? Communications { get; set; }

    }
}