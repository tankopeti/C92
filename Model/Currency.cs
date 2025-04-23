using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class Currency
    {
        [Key]
        public int CurrencyId { get; set; }

        [Required, StringLength(100)]
        public string? CurrencyName { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal ExchangeRate { get; set; }

        public bool IsBaseCurrency { get; set; }

        [Required]
        public string? CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ApplicationUser Creator { get; set; }
        public ApplicationUser LastModifier { get; set; }
    }
}

// Fields:
// CurrencyId: Primary key.
// Code: ISO currency code (e.g., “USD”, “EUR”).
// Name: Full name (e.g., “US Dollar”, “Euro”).
// ExchangeRate: Rate relative to the base currency (e.g., 1.0 for USD if base, 0.85 for EUR).
// IsBaseCurrency: Indicates the primary currency for conversions (true for one currency, false for others).
// Auditing: CreatedBy, LastModifiedBy, CreatedAt, UpdatedAt.
// Navigation: Creator, LastModifier.
// Purpose:
// Supports multi-currency pricing in ProductPrice:
// Example: Product A priced at $10/Pack (USD) and €8.50/Pack (EUR), linked via ProductPrice.CurrencyId.
// Enables currency conversions for transactional documents:
// Quotes: Display prices in the customer’s currency, converting via ExchangeRate.
// Orders, Invoices: Bill in the selected currency.
// Maintains financial consistency across the ERP system (e.g., for receipts).
// Does not directly affect file storage (ProductFile) but enhances documents like quotes that include product files (e.g., PDFs) by showing prices in relevant currencies.
// Relation to Previous Models:
// Product: Indirectly related via ProductPrice, which prices products in currencies.
// ProductFile: No direct link, but currency-aware quotes/orders may include files (e.g., a USD quote with a product manual PDF).
// ProductPrice: Uses CurrencyId to specify the currency for PurchasePrice and SalesPrice.
// UnitOfMeasurement, ProductUOM: Independent, but currencies apply to prices for units defined in these models.