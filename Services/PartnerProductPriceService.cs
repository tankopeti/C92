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
    public class PartnerProductPriceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PartnerProductPriceService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Get all active partner product prices
        public async Task<List<PartnerProductPrice>> GetActivePartnerProductPricesAsync()
        {
            return await _context.PartnerProductPrice
                .Include(ppp => ppp.Partner)
                .Include(ppp => ppp.Product)
                .Where(ppp => ppp.IsActive)
                .ToListAsync();
        }

        // Get partner product price by ID
        public async Task<PartnerProductPrice> GetPartnerProductPriceByIdAsync(int id)
        {
            var partnerProductPrice = await _context.PartnerProductPrice
                .Include(ppp => ppp.Partner)
                .Include(ppp => ppp.Product)
                .FirstOrDefaultAsync(ppp => ppp.PartnerProductPriceId == id && ppp.IsActive);

            if (partnerProductPrice == null)
            {
                throw new KeyNotFoundException("Partner product price not found or inactive.");
            }

            return partnerProductPrice;
        }

        // Get partner product price by PartnerId and ProductId
        public async Task<PartnerProductPrice> GetPartnerProductPriceByPartnerAndProductAsync(int partnerId, int productId)
        {
            var partnerProductPrice = await _context.PartnerProductPrice
                .Include(ppp => ppp.Partner)
                .Include(ppp => ppp.Product)
                .FirstOrDefaultAsync(ppp => ppp.PartnerId == partnerId && ppp.ProductId == productId && ppp.IsActive);

            if (partnerProductPrice == null)
            {
                throw new KeyNotFoundException("Partner product price not found or inactive.");
            }

            return partnerProductPrice;
        }

        // Create a new partner product price
        public async Task<PartnerProductPrice> CreatePartnerProductPriceAsync(PartnerProductPrice partnerProductPrice)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            // Validate foreign keys
            if (!await _context.Partners.AnyAsync(p => p.PartnerId == partnerProductPrice.PartnerId))
            {
                throw new ArgumentException("Invalid PartnerId.");
            }
            if (!await _context.Products.AnyAsync(p => p.ProductId == partnerProductPrice.ProductId))
            {
                throw new ArgumentException("Invalid ProductId.");
            }

            // Check for existing active price for the same PartnerId and ProductId
            if (await _context.PartnerProductPrice.AnyAsync(ppp => ppp.PartnerId == partnerProductPrice.PartnerId && ppp.ProductId == partnerProductPrice.ProductId && ppp.IsActive))
            {
                throw new ArgumentException("An active partner product price already exists for this PartnerId and ProductId.");
            }

            partnerProductPrice.CreatedBy = userId;
            partnerProductPrice.CreatedAt = DateTime.UtcNow;
            partnerProductPrice.IsActive = true;

            _context.PartnerProductPrice.Add(partnerProductPrice);
            await _context.SaveChangesAsync();
            return partnerProductPrice;
        }

        // Update an existing partner product price
        public async Task<PartnerProductPrice> UpdatePartnerProductPriceAsync(int id, PartnerProductPrice updatedPrice)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var partnerProductPrice = await _context.PartnerProductPrice.FindAsync(id);
            if (partnerProductPrice == null || !partnerProductPrice.IsActive)
            {
                throw new KeyNotFoundException("Partner product price not found or inactive.");
            }

            // Validate foreign keys
            if (!await _context.Partners.AnyAsync(p => p.PartnerId == updatedPrice.PartnerId))
            {
                throw new ArgumentException("Invalid PartnerId.");
            }
            if (!await _context.Products.AnyAsync(p => p.ProductId == updatedPrice.ProductId))
            {
                throw new ArgumentException("Invalid ProductId.");
            }

            // Check for existing active price for the same PartnerId and ProductId (excluding current record)
            if (await _context.PartnerProductPrice.AnyAsync(ppp => ppp.PartnerId == updatedPrice.PartnerId && ppp.ProductId == updatedPrice.ProductId && ppp.IsActive && ppp.PartnerProductPriceId != id))
            {
                throw new ArgumentException("An active partner product price already exists for this PartnerId and ProductId.");
            }

            partnerProductPrice.PartnerId = updatedPrice.PartnerId;
            partnerProductPrice.ProductId = updatedPrice.ProductId;
            partnerProductPrice.PartnerUnitPrice = updatedPrice.PartnerUnitPrice;
            partnerProductPrice.LastModifiedBy = userId;
            partnerProductPrice.UpdatedAt = DateTime.UtcNow;
            partnerProductPrice.IsActive = updatedPrice.IsActive;

            await _context.SaveChangesAsync();
            return partnerProductPrice;
        }

        // Soft delete a partner product price
        public async Task DeletePartnerProductPriceAsync(int id)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var partnerProductPrice = await _context.PartnerProductPrice.FindAsync(id);
            if (partnerProductPrice == null || !partnerProductPrice.IsActive)
            {
                throw new KeyNotFoundException("Partner product price not found or already inactive.");
            }

            partnerProductPrice.IsActive = false;
            partnerProductPrice.LastModifiedBy = userId;
            partnerProductPrice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}