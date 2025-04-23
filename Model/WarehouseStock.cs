using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class WarehouseStock
    {
        [Key]
        public int WarehouseStockId { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int UnitOfMeasurementId { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public Warehouse Warehouse { get; set; }
        public Product Product { get; set; }
        public UnitOfMeasurement UnitOfMeasurement { get; set; }
        public ApplicationUser Creator { get; set; }
        public ApplicationUser LastModifier { get; set; }
    }
}

// Fields:
// WarehouseStockId: Primary key.
// WarehouseId: Foreign key to Warehouse, identifying the storage location.
// ProductId: Foreign key to Product, linking to a specific product.
// UnitOfMeasurementId: Foreign key to UnitOfMeasurement, specifying the unit for StockQuantity (e.g., Pack, Pallet).
// StockQuantity: Number of units in stock (e.g., 100 Packs).
// Auditing: CreatedBy, LastModifiedBy, CreatedAt, UpdatedAt.
// Navigation: Warehouse, Product, UnitOfMeasurement, Creator, LastModifier.
// Purpose:
// Tracks inventory per product, warehouse, and unit:
// Example: Product A in Warehouse 1 has 100 Packs (UnitOfMeasurementId = Pack), equivalent to 600 Pieces via ProductUOM.ConversionFactor.
// Supports packaging units by allowing StockQuantity in units defined in ProductUOM (e.g., Packs, Pallets), not just the Product.BaseUOMId.
// Integrates with transactional documents:
// Quotes, Orders: Checks stock availability (e.g., enough Packs for an order).
// Delivery Notes: Updates stock when products are shipped (e.g., reduce 10 Packs).
// Warehouse Movements: Adjusts stock between warehouses (e.g., move 50 Packs).
// Complements file storage by enabling warehouse interfaces to display product files:
// Example: Show a PDF manual (ProductFile.FileCategory = “Manual”) for handling Product A during stock checks.
// Ensures inventory consistency with Product.StockQuantity (total base units across warehouses).
// Relation to Previous Models:
// Product: Links via ProductId, tracking inventory for products with associated files (e.g., PDFs).
// ProductFile: Indirectly related; warehouse staff can access product files (e.g., images for verification) during stock operations.
// ProductPrice: No direct link, but stock availability informs pricing decisions in quotes/orders.
// UnitOfMeasurement: Defines the unit for StockQuantity (e.g., Pack).
// ProductUOM: Validates UnitOfMeasurementId and provides conversion factors (e.g., 1 Pack = 6 Pieces).
// Currency: Independent, but stock data supports multi-currency transactions.