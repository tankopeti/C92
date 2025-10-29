using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Cloud9_2.Services
{
    public class ProductPriceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductPriceService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Get all active product prices
        public async Task<List<ProductPrice>> GetActiveProductPricesAsync()
        {
            return await _context.ProductPrices
                .Include(pp => pp.Product)
                .Include(pp => pp.UnitOfMeasurement)
                .Include(pp => pp.Currency)
                .Where(pp => pp.IsActive)
                .ToListAsync();
        }

        // Get product price by ID
        public async Task<ProductPrice> GetProductPriceByIdAsync(int id)
        {
            var productPrice = await _context.ProductPrices
                .Include(pp => pp.Product)
                .Include(pp => pp.UnitOfMeasurement)
                .Include(pp => pp.Currency)
                .FirstOrDefaultAsync(pp => pp.ProductPriceId == id && pp.IsActive);

            if (productPrice == null)
            {
                throw new KeyNotFoundException("Product price not found or inactive.");
            }

            return productPrice;
        }

        // Create a new product price
        public async Task<ProductPrice> CreateProductPriceAsync(ProductPrice productPrice)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            // Validate foreign keys
            if (!await _context.Products.AnyAsync(p => p.ProductId == productPrice.ProductId))
            {
                throw new ArgumentException("Invalid ProductId.");
            }
            if (!await _context.UnitsOfMeasurement.AnyAsync(u => u.UnitOfMeasurementId == productPrice.UnitOfMeasurementId))
            {
                throw new ArgumentException("Invalid UnitOfMeasurementId.");
            }
            if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == productPrice.CurrencyId))
            {
                throw new ArgumentException("Invalid CurrencyId.");
            }

            productPrice.CreatedBy = userId;
            productPrice.CreatedAt = DateTime.UtcNow;
            productPrice.IsActive = true;

            _context.ProductPrices.Add(productPrice);
            await _context.SaveChangesAsync();
            return productPrice;
        }

        // Update an existing product price
        public async Task<ProductPrice> UpdateProductPriceAsync(int id, ProductPrice updatedPrice)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var productPrice = await _context.ProductPrices.FindAsync(id);
            if (productPrice == null || !productPrice.IsActive)
            {
                throw new KeyNotFoundException("Product price not found or inactive.");
            }

            // Validate foreign keys
            if (!await _context.Products.AnyAsync(p => p.ProductId == updatedPrice.ProductId))
            {
                throw new ArgumentException("Invalid ProductId.");
            }
            if (!await _context.UnitsOfMeasurement.AnyAsync(u => u.UnitOfMeasurementId == updatedPrice.UnitOfMeasurementId))
            {
                throw new ArgumentException("Invalid UnitOfMeasurementId.");
            }
            if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == updatedPrice.CurrencyId))
            {
                throw new ArgumentException("Invalid CurrencyId.");
            }

            productPrice.ProductId = updatedPrice.ProductId;
            productPrice.UnitOfMeasurementId = updatedPrice.UnitOfMeasurementId;
            productPrice.CurrencyId = updatedPrice.CurrencyId;
            productPrice.PurchasePrice = updatedPrice.PurchasePrice;
            productPrice.SalesPrice = updatedPrice.SalesPrice;
            productPrice.DiscountPercentage = updatedPrice.DiscountPercentage;
            productPrice.Volume1 = updatedPrice.Volume1;
            productPrice.Volume1Price = updatedPrice.Volume1Price;
            productPrice.Volume2 = updatedPrice.Volume2;
            productPrice.Volume2Price = updatedPrice.Volume2Price;
            productPrice.Volume3 = updatedPrice.Volume3;
            productPrice.Volume3Price = updatedPrice.Volume3Price;
            productPrice.StartDate = updatedPrice.StartDate;
            productPrice.EndDate = updatedPrice.EndDate;
            productPrice.IsActive = updatedPrice.IsActive;
            productPrice.LastModifiedBy = userId;
            productPrice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return productPrice;
        }

        // Soft delete a product price
        public async Task DeleteProductPriceAsync(int id)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var productPrice = await _context.ProductPrices.FindAsync(id);
            if (productPrice == null || !productPrice.IsActive)
            {
                throw new KeyNotFoundException("Product price not found or already inactive.");
            }

            productPrice.IsActive = false;
            productPrice.LastModifiedBy = userId;
            productPrice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // Get volume price based on quantity
        public async Task<decimal> GetVolumePriceAsync(int productPriceId, int quantity)
        {
            var productPrice = await _context.ProductPrices
                .FirstOrDefaultAsync(pp => pp.ProductPriceId == productPriceId && pp.IsActive);

            if (productPrice == null)
            {
                throw new KeyNotFoundException("Product price not found or inactive.");
            }

            // Check volume thresholds and return the appropriate price
            if (quantity >= productPrice.Volume3 && productPrice.Volume3 > 0 && productPrice.Volume3Price > 0)
            {
                return productPrice.Volume3Price;
            }
            else if (quantity >= productPrice.Volume2 && productPrice.Volume2 > 0 && productPrice.Volume2Price > 0)
            {
                return productPrice.Volume2Price;
            }
            else if (quantity >= productPrice.Volume1 && productPrice.Volume1 > 0 && productPrice.Volume1Price > 0)
            {
                return productPrice.Volume1Price;
            }

            // Apply discount if applicable
            decimal finalPrice = productPrice.SalesPrice;
            if (productPrice.DiscountPercentage > 0)
            {
                finalPrice -= finalPrice * (productPrice.DiscountPercentage / 100);
            }

            return finalPrice;
        }
    }
}