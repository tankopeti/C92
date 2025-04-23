using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class ProductUOM
    {
        [Key]
        public int ProductUOMId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int UnitOfMeasurementId { get; set; }

        [Required, Range(0, double.MaxValue)]
        public double ConversionFactor { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public Product Product { get; set; }
        public UnitOfMeasurement UnitOfMeasurement { get; set; }
        public ApplicationUser Creator { get; set; }
        public ApplicationUser LastModifier { get; set; }
    }
}

// ProductUOMId: Primary key.
// ProductId: Foreign key to Product, linking to a specific product.
// UnitOfMeasurementId: Foreign key to UnitOfMeasurement, specifying the unit (e.g., Pack, Pallet).
// ConversionFactor: Number of base units per unit (e.g., 6 for 1 Pack = 6 Pieces).
// Auditing: CreatedBy, LastModifiedBy, CreatedAt, UpdatedAt.
// Navigation: Product, UnitOfMeasurement, Creator, LastModifier.
// Purpose:
// Defines how a product is measured in different units, supporting packaging units:
// Example: For Product A with BaseUOMId = Piece, a ProductUOM record might have UnitOfMeasurementId = Pack, ConversionFactor = 6.
// Another record: UnitOfMeasurementId = Pallet, ConversionFactor = 600.
// Validates units used in:
// ProductPrice: Ensures pricing units (e.g., $10/Pack) are valid for the product.
// WarehouseStock: Tracks inventory in units like Pack or Pallet.
// **QuoteItem, OrderItem, InvoiceItem`, etc.: Specifies quantities in chosen units.
// Links to ProductFile via ProductUOMId, allowing files (e.g., PDFs) to be associated with specific packaging units (e.g., manual for “Pack”).
// Supports conversions for inventory and reporting (e.g., 100 Packs = 600 Pieces).
// Relation to Previous Models:
// Product: Defines valid units for a product beyond BaseUOMId.
// ProductFile: ProductUOMId enables packaging unit-specific files (e.g., PDF for “Pack” vs. “Pallet”).
// ProductPrice: Ensures UnitOfMeasurementId in prices matches a ProductUOM record.
// UnitOfMeasurement: Provides the units (e.g., Pack, Piece) used in UnitOfMeasurementId.