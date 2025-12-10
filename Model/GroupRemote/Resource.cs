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
        public string? CreatedAt { get; set; }

        public string? Comment1 { get; set; }
        public string? Comment2 { get; set; }
    }

    public class ResourceDto
    {
        public int ResourceId { get; set; }
        public string? Name { get; set; }
        public int? ResourceTypeId { get; set; }
        public string? ResourceTypeName { get; set; }
        public int? ResourceStatusId { get; set; }
        public string? ResourceStatusName { get; set; }
        public string? Serial { get; set; }
        public DateTime? NextService { get; set; }
        public DateTime? DateOfPurchase { get; set; }
        public int? WarrantyPeriod { get; set; }
        public DateTime? WarrantyExpireDate { get; set; }
        public DateTime? ServiceDate { get; set; }
        public string? WhoBuyId { get; set; }
        public string? WhoBuyName { get; set; }
        public string? WhoLastServicedId { get; set; }
        public string? WhoLastServicedName { get; set; }
        public int? PartnerId { get; set; }
        public string? PartnerName { get; set; }
        public int? SiteId { get; set; }
        public string? SiteName { get; set; }
        public int? ContactId { get; set; }
        public string? ContactName { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public decimal? Price { get; set; }
        public DateTime? CreatedDate { get; set; }
        public bool? IsActive { get; set; } = true;
        public string? Comment1 { get; set; }
        public string? Comment2 { get; set; }
        
        public string? CreatedAt { get; set; }

        // Summary of history
        public int HistoryCount { get; set; }
        public string? LastChangeDescription { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string? LastModifiedByName { get; set; }
        public
        decimal? LastServicePrice
        { get; set; }
    }

    public class CreateResourceDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        public int? ResourceTypeId { get; set; }
        public int? ResourceStatusId { get; set; }

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
        public string? WhoLastServicedId { get; set; }

        public int? PartnerId { get; set; }
        public int? SiteId { get; set; }
        public int? ContactId { get; set; }
        public int? EmployeeId { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Price must be between 0 and 999,999.99")]
        public decimal? Price { get; set; }

        public bool IsActive { get; set; } = true;

        public string? Comment1 { get; set; }
        public string? Comment2 { get; set; }
        public string? CreatedAt { get; set; }
    }

    public class UpdateResourceDto
    {
        [Required]
        public int ResourceId { get; set; }

        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        public int? ResourceTypeId { get; set; }
        public int? ResourceStatusId { get; set; }

        [StringLength(100, ErrorMessage = "Serial cannot exceed 100 characters")]
        public string? Serial { get; set; }

        [Display(Name = "Next Service")]
        public DateTime? NextService { get; set; }

        [Display(Name = "Date of Purchase")]
        public DateTime? DateOfPurchase { get; set; }

        [Display(Name = "Warranty Period")]
        public int? WarrantyPeriod { get; set; }

        [Display(Name = "Warranty Expire Date")]
        public DateTime? WarrantyExpireDate { get; set; }

        [Display(Name = "Service Date")]
        public DateTime? ServiceDate { get; set; }

        public string? WhoBuyId { get; set; }
        public string? WhoLastServicedId { get; set; }

        public int? PartnerId { get; set; }
        public int? SiteId { get; set; }
        public int? ContactId { get; set; }
        public int? EmployeeId { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Price must be between 0 and 999,999.99")]
        public decimal? Price { get; set; }

        public bool? IsActive { get; set; }

        public string? Comment1 { get; set; }
        public string? Comment2 { get; set; }
    }

}