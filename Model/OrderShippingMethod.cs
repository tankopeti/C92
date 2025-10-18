using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cloud9_2.Models
{
    public class OrderShippingMethod
    {
        [Key]
        public int ShippingMethodId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Szállítási mód")]
        public string MethodName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Leírás")]
        public string? Description { get; set; }

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

        public ICollection<Order>? Orders { get; set; }
    }

    public class OrderShippingMethodDTO
{
    public int ShippingMethodId { get; set; }
    public string MethodName { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class OrderShippingMethodCreateDTO
{
    [Required]
    [StringLength(100)]
    public string MethodName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class OrderShippingMethodUpdateDTO
{
    [Required]
    public int ShippingMethodId { get; set; }

    [Required]
    [StringLength(100)]
    public string MethodName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

}