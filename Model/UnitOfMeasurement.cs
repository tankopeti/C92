using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class UnitOfMeasurement
    {
        [Key]
        public int UnitOfMeasurementId { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; }

        [Required, StringLength(50)]
        public string Type { get; set; }

        public bool IsBaseUnit { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ApplicationUser Creator { get; set; }
        public ApplicationUser LastModifier { get; set; }
    }
}

// Fields:
// UnitOfMeasurementId: Primary key.
// Name: Unit name (e.g., “Piece”, “Pack”, “Kilogram”).
// Type: Category of unit (e.g., “Quantity”, “Packaging”, “Weight”, “Dimension”).
// IsBaseUnit: Indicates if the unit is the base for conversions (e.g., true for “Piece”, false for “Pack”).
// Auditing: CreatedBy, LastModifiedBy, CreatedAt, UpdatedAt.
// Navigation: Creator, LastModifier.
// Purpose:
// Defines units used across the application:
// Product: BaseUOMId, WeightUOMId, DimensionUOMId reference UnitOfMeasurement for inventory and physical attributes.
// ProductPrice: UnitOfMeasurementId specifies the unit for pricing (e.g., $10/Pack).
// ProductUOM: Maps products to units (e.g., Pack = 6 Pieces) for packaging units.
// WarehouseStock: UnitOfMeasurementId tracks inventory units (e.g., 100 Packs).
// ProductFile: ProductUOMId links files to packaging units (e.g., PDF for “Pack”).
// Supports packaging units by allowing units like “Pack” or “Pallet” with Type = “Packaging”.
// Enables unit conversions via ProductUOM.ConversionFactor (e.g., 1 Pack = 6 Pieces).
// Relation to Previous Models:
// Product: Provides units for BaseUOMId (e.g., Piece), ensuring inventory consistency.
// ProductFile: Indirectly related via ProductUOMId, allowing files (e.g., PDFs) to be associated with specific units.
// ProductPrice: Defines the unit for pricing (e.g., Piece, Pack), validated against ProductUOM.