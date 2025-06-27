using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DiscountService> _logger;

        public DiscountService(ApplicationDbContext context, ILogger<DiscountService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DiscountDto> GetDiscountAsync(string entityType, int itemId)
        {
            try
            {
                _logger.LogInformation("Fetching discount for {EntityType} ItemId: {ItemId}", entityType, itemId);

                DiscountDto discount = null;
                switch (entityType.ToLower())
                {
                    case "quote":
                        discount = await _context.QuoteItemDiscounts
                            .Where(d => d.QuoteItemId == itemId)
                            .Select(d => new DiscountDto
                            {
                                DiscountId = d.QuoteItemDiscountId,
                                ItemId = d.QuoteItemId,
                                DiscountType = d.DiscountType.ToString(),
                                DiscountPercentage = d.DiscountPercentage,
                                DiscountAmount = d.DiscountAmount,
                                BasePrice = d.BasePrice,
                                PartnerPrice = d.PartnerPrice,
                                VolumeThreshold = d.VolumeThreshold,
                                VolumePrice = d.VolumePrice
                            })
                            .FirstOrDefaultAsync();
                        break;
                    default:
                        throw new ArgumentException($"Invalid entity type: {entityType}");
                }

                if (discount == null)
                {
                    _logger.LogWarning("Discount not found for {EntityType} ItemId: {ItemId}", entityType, itemId);
                    return null;
                }

                _logger.LogInformation("Retrieved discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return discount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                throw;
            }
        }

        public async Task<DiscountDto> CreateDiscountAsync(string entityType, DiscountDto discountDto)
        {
            if (discountDto == null)
                throw new ArgumentNullException(nameof(discountDto));

            try
            {
                _logger.LogInformation("Creating discount for {EntityType} ItemId: {ItemId}", entityType, discountDto.ItemId);

                decimal productId = 0;
                int partnerId = 0;
                decimal quantity = 0;
                decimal basePrice = 0;

                switch (entityType.ToLower())
                {
                    case "quote":
                        var quoteItem = await _context.QuoteItems
                            .Include(qi => qi.Quote)
                            .Include(qi => qi.Discount)
                            .FirstOrDefaultAsync(qi => qi.QuoteItemId == discountDto.ItemId);
                        if (quoteItem == null)
                            throw new ArgumentException($"QuoteItem with ID {discountDto.ItemId} not found");
                        if (await _context.QuoteItemDiscounts.AnyAsync(d => d.QuoteItemId == discountDto.ItemId))
                            throw new ArgumentException($"Discount already exists for QuoteItemId {discountDto.ItemId}");
                        productId = quoteItem.ProductId;
                        partnerId = quoteItem.Quote.PartnerId;
                        quantity = quoteItem.Quantity;
                        basePrice = quoteItem.NetDiscountedPrice; // Use QuoteItem.ListPrice
                        break;
                    case "order":
                        var orderItem = await _context.OrderItems
                            .Include(oi => oi.Order)
                            .FirstOrDefaultAsync(oi => oi.OrderItemId == discountDto.ItemId);
                        if (orderItem == null)
                            throw new ArgumentException($"OrderItem with ID {discountDto.ItemId} not found");
                        productId = orderItem.ProductId;
                        partnerId = orderItem.Order.PartnerId;
                        quantity = orderItem.Quantity;
                        basePrice = orderItem.UnitPrice;
                        break;
                    default:
                        throw new ArgumentException($"Invalid entity type: {entityType}");
                }

                var productPrice = await _context.ProductPrices
                    .Where(pp => pp.ProductId == productId && pp.IsActive)
                    .FirstOrDefaultAsync();
                var partnerPrice = await _context.PartnerProductPrice
                    .Where(pp => pp.ProductId == productId && pp.PartnerId == partnerId)
                    .FirstOrDefaultAsync();

                decimal unitPrice = basePrice;

                switch (discountDto.DiscountType)
                {
                    case "ListPrice":
                        unitPrice = basePrice;
                        discountDto.DiscountPercentage = null;
                        discountDto.DiscountAmount = null;
                        discountDto.PartnerPrice = null;
                        discountDto.VolumeThreshold = null;
                        discountDto.VolumePrice = null;
                        break;
                    case "CustomDiscountPercentage":
                        discountDto.DiscountPercentage = discountDto.DiscountPercentage ?? 0;
                        unitPrice = basePrice * (1 - discountDto.DiscountPercentage.Value / 100);
                        discountDto.DiscountAmount = null;
                        discountDto.PartnerPrice = null;
                        discountDto.VolumeThreshold = null;
                        discountDto.VolumePrice = null;
                        break;
                    case "CustomDiscountAmount":
                        discountDto.DiscountAmount = discountDto.DiscountAmount ?? 0;
                        unitPrice = (basePrice * quantity - discountDto.DiscountAmount.Value) / quantity;
                        discountDto.DiscountPercentage = null;
                        discountDto.PartnerPrice = null;
                        discountDto.VolumeThreshold = null;
                        discountDto.VolumePrice = null;
                        break;
                    case "PartnerPrice":
                        basePrice = partnerPrice?.PartnerUnitPrice ?? basePrice;
                        unitPrice = basePrice;
                        discountDto.PartnerPrice = basePrice;
                        discountDto.DiscountPercentage = null;
                        discountDto.DiscountAmount = null;
                        discountDto.VolumeThreshold = null;
                        discountDto.VolumePrice = null;
                        break;
                    case "VolumeDiscount":
                        if (quantity >= productPrice?.Volume3 && productPrice?.Volume3Price > 0)
                        {
                            basePrice = productPrice.Volume3Price;
                            discountDto.VolumeThreshold = productPrice.Volume3;
                            discountDto.VolumePrice = productPrice.Volume3Price;
                        }
                        else if (quantity >= productPrice?.Volume2 && productPrice?.Volume2Price > 0)
                        {
                            basePrice = productPrice.Volume2Price;
                            discountDto.VolumeThreshold = productPrice.Volume2;
                            discountDto.VolumePrice = productPrice.Volume2Price;
                        }
                        else if (quantity >= productPrice?.Volume1 && productPrice?.Volume1Price > 0)
                        {
                            basePrice = productPrice.Volume1Price;
                            discountDto.VolumeThreshold = productPrice.Volume1;
                            discountDto.VolumePrice = productPrice.Volume1Price;
                        }
                        unitPrice = basePrice;
                        discountDto.DiscountPercentage = null;
                        discountDto.DiscountAmount = null;
                        discountDto.PartnerPrice = null;
                        break;
                    default:
                        throw new ArgumentException($"Invalid discount type: {discountDto.DiscountType}");
                }
                discountDto.BasePrice = basePrice;

                object discountEntity = null;
                switch (entityType.ToLower())
                {
                    case "quote":
                        discountEntity = new QuoteItemDiscount
                        {
                            QuoteItemId = discountDto.ItemId,
                            DiscountType = Enum.Parse<DiscountType>(discountDto.DiscountType),
                            DiscountPercentage = discountDto.DiscountPercentage,
                            DiscountAmount = discountDto.DiscountAmount,
                            BasePrice = discountDto.BasePrice,
                            PartnerPrice = discountDto.PartnerPrice,
                            VolumeThreshold = discountDto.VolumeThreshold,
                            VolumePrice = discountDto.VolumePrice
                        };
                        _context.QuoteItemDiscounts.Add((QuoteItemDiscount)discountEntity);
                        break;
                }

                switch (entityType.ToLower())
                {
                    case "quote":
                        var quoteItem = await _context.QuoteItems.FindAsync(discountDto.ItemId);
                        quoteItem.NetDiscountedPrice = unitPrice;
                        quoteItem.TotalPrice = unitPrice * quantity;
                        break;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Created discount for {EntityType} ItemId: {ItemId}", entityType, discountDto.ItemId);
                return discountDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating discount for {EntityType} ItemId: {ItemId}", entityType, discountDto.ItemId);
                throw;
            }
        }

        public async Task<DiscountDto> UpdateDiscountAsync(string entityType, int itemId, DiscountDto discountDto)
        {
            if (discountDto == null)
                throw new ArgumentNullException(nameof(discountDto));

            try
            {
                _logger.LogInformation("Updating discount for {EntityType} ItemId: {ItemId}", entityType, itemId);

                decimal productId = 0;
                int partnerId = 0;
                decimal quantity = 0;
                decimal basePrice = 0;

                object discountEntity = null;
                switch (entityType.ToLower())
                {
                    case "quote":
                        discountEntity = await _context.QuoteItemDiscounts
                            .FirstOrDefaultAsync(d => d.QuoteItemId == itemId);
                        if (discountEntity == null)
                            throw new ArgumentException($"Discount not found for QuoteItemId {itemId}");
                        var quoteItem = await _context.QuoteItems
                            .Include(qi => qi.Quote)
                            .FirstOrDefaultAsync(qi => qi.QuoteItemId == itemId);
                        productId = quoteItem.ProductId;
                        partnerId = quoteItem.Quote.PartnerId;
                        quantity = quoteItem.Quantity;
                        basePrice = quoteItem.NetDiscountedPrice;
                        break;
                    default:
                        throw new ArgumentException($"Invalid entity type: {entityType}");
                }

                var productPrice = await _context.ProductPrices
                    .Where(pp => pp.ProductId == productId && pp.IsActive)
                    .FirstOrDefaultAsync();
                var partnerPrice = await _context.PartnerProductPrice
                    .Where(pp => pp.ProductId == productId && pp.PartnerId == partnerId)
                    .FirstOrDefaultAsync();

                decimal unitPrice = basePrice;

                switch (discountDto.DiscountType)
                {
                    case "ListPrice":
                        unitPrice = basePrice;
                        discountDto.DiscountPercentage = null;
                        discountDto.DiscountAmount = null;
                        discountDto.PartnerPrice = null;
                        discountDto.VolumeThreshold = null;
                        discountDto.VolumePrice = null;
                        break;
                    case "CustomDiscountPercentage":
                        discountDto.DiscountPercentage = discountDto.DiscountPercentage ?? 0;
                        unitPrice = basePrice * (1 - discountDto.DiscountPercentage.Value / 100);
                        discountDto.DiscountAmount = null;
                        discountDto.PartnerPrice = null;
                        discountDto.VolumeThreshold = null;
                        discountDto.VolumePrice = null;
                        break;
                    case "CustomDiscountAmount":
                        discountDto.DiscountAmount = discountDto.DiscountAmount ?? 0;
                        unitPrice = (basePrice * quantity - discountDto.DiscountAmount.Value) / quantity;
                        discountDto.DiscountPercentage = null;
                        discountDto.PartnerPrice = null;
                        discountDto.VolumeThreshold = null;
                        discountDto.VolumePrice = null;
                        break;
                    case "PartnerPrice":
                        basePrice = partnerPrice?.PartnerUnitPrice ?? basePrice;
                        unitPrice = basePrice;
                        discountDto.PartnerPrice = basePrice;
                        discountDto.DiscountPercentage = null;
                        discountDto.DiscountAmount = null;
                        discountDto.VolumeThreshold = null;
                        discountDto.VolumePrice = null;
                        break;
                    case "VolumeDiscount":
                        if (quantity >= productPrice?.Volume3 && productPrice?.Volume3Price > 0)
                        {
                            basePrice = productPrice.Volume3Price;
                            discountDto.VolumeThreshold = productPrice.Volume3;
                            discountDto.VolumePrice = productPrice.Volume3Price;
                        }
                        else if (quantity >= productPrice?.Volume2 && productPrice?.Volume2Price > 0)
                        {
                            basePrice = productPrice.Volume2Price;
                            discountDto.VolumeThreshold = productPrice.Volume2;
                            discountDto.VolumePrice = productPrice.Volume2Price;
                        }
                        else if (quantity >= productPrice?.Volume1 && productPrice?.Volume1Price > 0)
                        {
                            basePrice = productPrice.Volume1Price;
                            discountDto.VolumeThreshold = productPrice.Volume1;
                            discountDto.VolumePrice = productPrice.Volume1Price;
                        }
                        unitPrice = basePrice;
                        discountDto.DiscountPercentage = null;
                        discountDto.DiscountAmount = null;
                        discountDto.PartnerPrice = null;
                        break;
                    default:
                        throw new ArgumentException($"Invalid discount type: {discountDto.DiscountType}");
                }
                discountDto.BasePrice = basePrice;

                switch (entityType.ToLower())
                {
                    case "quote":
                        var quoteDiscount = (QuoteItemDiscount)discountEntity;
                        quoteDiscount.DiscountType = Enum.Parse<DiscountType>(discountDto.DiscountType);
                        quoteDiscount.DiscountPercentage = discountDto.DiscountPercentage;
                        quoteDiscount.DiscountAmount = discountDto.DiscountAmount;
                        quoteDiscount.BasePrice = discountDto.BasePrice;
                        quoteDiscount.PartnerPrice = discountDto.PartnerPrice;
                        quoteDiscount.VolumeThreshold = discountDto.VolumeThreshold;
                        quoteDiscount.VolumePrice = discountDto.VolumePrice;
                        var quoteItemUpdate = await _context.QuoteItems.FindAsync(itemId);
                        quoteItemUpdate.NetDiscountedPrice = unitPrice;
                        quoteItemUpdate.TotalPrice = unitPrice * quantity;
                        break;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return discountDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                throw;
            }
        }

        public async Task<bool> DeleteDiscountAsync(string entityType, int entityId)
        {
            try
            {
                _logger.LogInformation("Deleting discount for {EntityType} EntityId: {EntityId}", entityType, entityId);

                bool discountExists = false;
                switch (entityType.ToLower())
                {
                    case "quoteitem":
                        var quoteDiscount = await _context.QuoteItemDiscounts
                            .FirstOrDefaultAsync(d => d.QuoteItemId == entityId);
                        if (quoteDiscount != null)
                        {
                            _context.QuoteItemDiscounts.Remove(quoteDiscount);
                            discountExists = true;
                        }
                        break;
                    default:
                        throw new ArgumentException($"Invalid entity type: {entityType}");
                }

                if (!discountExists)
                {
                    _logger.LogWarning("Discount not found for {EntityType} EntityId: {EntityId}", entityType, entityId);
                    return false;
                }

                if (entityType.ToLower() == "quoteitem")
                {
                    var quoteItem = await _context.QuoteItems.FindAsync(entityId);
                    if (quoteItem != null)
                    {
                        var productPrice = await _context.ProductPrices
                            .Where(pp => pp.ProductId == quoteItem.ProductId && pp.IsActive)
                            .FirstOrDefaultAsync();
                        decimal basePrice = productPrice?.SalesPrice ?? quoteItem.NetDiscountedPrice;
                        quoteItem.NetDiscountedPrice = basePrice;
                        quoteItem.TotalPrice = basePrice * quoteItem.Quantity;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted discount for {EntityType} EntityId: {EntityId}", entityType, entityId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting discount for {EntityType} EntityId: {EntityId}", entityType, entityId);
                throw;
            }
        }

        private async Task<decimal> GetProductIdAsync(string entityType, int itemId)
        {
            switch (entityType.ToLower())
            {
                case "quoteitem":
                    return (await _context.QuoteItems.FindAsync(itemId))?.ProductId ?? 0;
                case "orderitem":
                    return (await _context.OrderItems.FindAsync(itemId))?.ProductId ?? 0;
                default:
                    throw new ArgumentException($"Invalid entity type: {entityType}");
            }
        }
    }
}