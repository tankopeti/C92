using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required, StringLength(100)]
        public string? SKU { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int? CategoryId { get; set; }

        [Required]
        public int? BaseUOMId { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int? ReorderLevel { get; set; }

        [StringLength(100)]
        public string? WarehouseLocation { get; set; }

        [Range(0, double.MaxValue)]
        public double? Weight { get; set; }

        [Required]
        public int? WeightUOMId { get; set; }

        [Range(0, double.MaxValue)]
        public double? Length { get; set; }

        [Range(0, double.MaxValue)]
        public double? Width { get; set; }

        [Range(0, double.MaxValue)]
        public double? Height { get; set; }

        [Required]
        public int? DimensionUOMId { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public string? CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public UnitOfMeasurement? BaseUOM { get; set; }
        public UnitOfMeasurement? WeightUOM { get; set; }
        public UnitOfMeasurement? DimensionUOM { get; set; }
        public ApplicationUser? Creator { get; set; }
        public ApplicationUser? LastModifier { get; set; }
        public Category? Category { get; set; }
        public List<ProductFile>? Files { get; set; } = new List<ProductFile>();
        public ICollection<QuoteItem> QuoteItems { get; set; }
    }
}

// ProductId: Primary key.
// SKU, Name, Description: Basic product details.
// CategoryId: Links to a product category (assumed model).
// BaseUOMId: Defines the base unit (e.g., Piece) for inventory and packaging units.
// StockQuantity: Inventory count in BaseUOMId units.
// ReorderLevel: Threshold for restocking.
// WarehouseLocation: Optional storage location.
// Weight, WeightUOMId, Length, Width, Height, DimensionUOMId: Physical attributes for shipping.
// IsActive: Soft delete flag.
// Auditing: CreatedBy, LastModifiedBy, CreatedAt, UpdatedAt.
// Navigation: BaseUOM, WeightUOM, DimensionUOM, Creator, LastModifier, Files.