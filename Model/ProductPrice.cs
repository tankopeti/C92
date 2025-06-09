using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class ProductPrice
    {
        [Key]
        public int ProductPriceId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int UnitOfMeasurementId { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal PurchasePrice { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal SalesPrice { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; }

        [Range(0, double.MaxValue)]
        public int Volume1 { get; set; }  // First volume threshold

        [Range(0, double.MaxValue)]
        public decimal Volume1Price { get; set; }  // Price for Volume1

        [Range(0, double.MaxValue)]
        public int Volume2 { get; set; }  // Second volume threshold

        [Range(0, double.MaxValue)]
        public decimal Volume2Price { get; set; }  // Price for Volume2

        [Range(0, double.MaxValue)]
        public int Volume3 { get; set; }  // Third volume threshold

        [Range(0, double.MaxValue)]
        public decimal Volume3Price { get; set; }  // Price for Volume3


        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public string CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public Product Product { get; set; }
        public UnitOfMeasurement UnitOfMeasurement { get; set; }
        public Currency Currency { get; set; }
        public ApplicationUser Creator { get; set; }
        public ApplicationUser LastModifier { get; set; }
    }
}

// ProductPriceId: Primary key.
// ProductId: Foreign key to Product, linking the price to a specific product.
// UnitOfMeasurementId: Foreign key to UnitOfMeasurement, specifying the unit for pricing (e.g., Piece, Pack, Pallet).
// CurrencyId: Foreign key to Currency, defining the currency (e.g., USD, EUR).
// PurchasePrice: Cost to acquire the product (in CurrencyId).
// SalesPrice: Selling price (in CurrencyId).
// DiscountPercentage: Optional discount (0–100%).
// StartDate, EndDate: Time interval for the price’s validity.
// IsActive: Soft delete flag for prices.
// Auditing: CreatedBy, LastModifiedBy, CreatedAt, UpdatedAt.
// Navigation: Product, UnitOfMeasurement, Currency, Creator, LastModifier.
// Purpose:
// Defines prices for products in specific units and currencies, supporting time-based pricing (e.g., $10/Pack from 2025-01-01 to 2025-12-31).
// Enables pricing for packaging units (e.g., Piece vs. Pack) by linking to UnitOfMeasurementId, validated against ProductUOM.
// Used in transactional documents:
// Quotes: QuoteItem.UnitPrice sourced from ProductPrice.SalesPrice.
// Orders: Pricing for OrderItem.
// Invoices: Billing amounts in InvoiceItem.
// Supports multi-currency pricing via CurrencyId, leveraging Currency.ExchangeRate.
// Relation to Previous Models:
// Product: Links via ProductId, allowing prices for products that have associated files (e.g., PDFs in ProductFile).
// ProductFile: Indirectly related; prices don’t reference files, but quotes/orders may display product PDFs alongside prices.
// Packaging Units: Supports pricing in packaging units (e.g., $10/Pack, $900/Pallet) defined in ProductUOM.