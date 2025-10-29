using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Resource
    {
        [Key]
        public int ResourceId { get; set; }

        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        public int? ResourceTypeId { get; set; }
        public ResourceType? ResourceType { get; set; }

        public int? ResourceStatusId { get; set; }
        public ResourceStatus? ResourceStatus { get; set; }

        [StringLength(100, ErrorMessage = "Serial cannot exceed 100 characters")]
        public string? Serial { get; set; }

        [Display(Name = "Next Service")]
        public DateTime? NextService { get; set; }

        [Display(Name = "Date of Purchase")]
        public DateTime? DateOfPurchase { get; set; }

        [Display(Name = "Warranty Period")]
        public int? WarrantyPeriod { get; set; } // In months

        [Display(Name = "Warranty Expire Date")]
        public DateTime? WarrantyExpireDate { get; set; }

        [Display(Name = "Service Date")]
        public DateTime? ServiceDate { get; set; }

        public string? WhoBuyId { get; set; }
        public ApplicationUser? WhoBuy { get; set; }

        public string? WhoLastServicedId { get; set; }
        public ApplicationUser? WhoLastServiced { get; set; }

        public int? PartnerId { get; set; }
        public Partner? Partner { get; set; }

        public int? SiteId { get; set; }
        public Site? Site { get; set; }

        public int? ContactId { get; set; }
        public Contact? Contact { get; set; }

        public int? EmployeeId { get; set; }
        public Employees? Employee { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Price must be between 0 and 999,999.99")]
        public decimal? Price { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<ResourceHistory> ResourceHistories { get; set; } = new List<ResourceHistory>();

        public bool? IsActive { get; set; } = true; // Matches database BIT NULL

        public string? Comment1 { get; set; }
        public string? Comment2 { get; set; }
    }

}