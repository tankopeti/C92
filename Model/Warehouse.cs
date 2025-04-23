using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Warehouse
    {
        [Key]
        public int WarehouseId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public string CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ApplicationUser Creator { get; set; }
        public ApplicationUser LastModifier { get; set; }
        public List<WarehouseStock> Stocks { get; set; } = new List<WarehouseStock>();
    }
}

// Fields:
// WarehouseId: Primary key, referenced by WarehouseStock.WarehouseId.
// Name: Warehouse identifier (e.g., “Main Warehouse”).
// Address, City, State, PostalCode, Country: Location details for logistics.
// IsActive: Soft delete flag for warehouses.
// Auditing: CreatedBy, LastModifiedBy, CreatedAt, UpdatedAt.
// Navigation: Creator, LastModifier, Stocks (one-to-many with WarehouseStock).
// Purpose:
// Represents a physical or logical storage location for products.
// Links to WarehouseStock to track inventory (e.g., 100 Packs of Product A in Main Warehouse).
// Supports warehouse management in transactional documents:
// Delivery Notes: Specifies warehouse for shipping.
// Warehouse Movements: Transfers stock between warehouses.
// Complements file storage by allowing warehouse interfaces to access product files (e.g., PDFs for handling instructions) via WarehouseStock.Product.Files.
// Relation to Other Models:
// WarehouseStock: One-to-many, WarehouseId links to stock entries.
// Product: Indirectly via WarehouseStock.ProductId, connecting warehouses to products with files.
// ProductFile: Indirectly via WarehouseStock.Product.Files, enabling PDFs (e.g., manuals) for inventory tasks.
// ProductUOM, UnitOfMeasurement: Supports inventory in packaging units (e.g., Packs) via WarehouseStock.UnitOfMeasurementId.
// ProductPrice, Currency: Independent, but stock levels inform pricing/availability.