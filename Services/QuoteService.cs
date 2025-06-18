using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Cloud9_2.Pages.CRM.Quotes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{
    public class QuoteService : IQuoteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuoteService> _logger;
        private readonly IOrderService _orderService;

        public QuoteService(ApplicationDbContext context, ILogger<QuoteService> logger, IOrderService orderService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        public async Task<string> GetNextQuoteNumberAsync()
        {
            var lastQuoteNumber = await _context.Quotes
                .Where(q => q.QuoteNumber != null && q.QuoteNumber.StartsWith("QUOTE-"))
                .OrderByDescending(q => q.QuoteNumber)
                .Select(q => q.QuoteNumber)
                .FirstOrDefaultAsync();

            string nextNumber = "QUOTE-0001";
            if (!string.IsNullOrEmpty(lastQuoteNumber) && int.TryParse(lastQuoteNumber.Replace("QUOTE-", ""), out int lastNumber))
            {
                nextNumber = $"QUOTE-{lastNumber + 1:D4}";
            }

            return nextNumber;
        }

        public async Task<bool> QuoteExistsAsync(int quoteId)
        {
            return await _context.Quotes.AnyAsync(q => q.QuoteId == quoteId);
        }

        public async Task<List<QuoteItemDto>> GetQuoteItemsAsync(int quoteId)
        {
            return await _context.QuoteItems
                .Where(qi => qi.QuoteId == quoteId)
                .Include(qi => qi.VatType)
                .Include(qi => qi.Discount)
                .Select(qi => new QuoteItemDto
                {
                    QuoteItemId = qi.QuoteItemId,
                    QuoteId = qi.QuoteId,
                    ProductId = qi.ProductId,
                    Quantity = qi.Quantity,
                    UnitPrice = qi.UnitPrice,
                    ItemDescription = qi.ItemDescription,
                    TotalPrice = qi.TotalPrice,
                    VatTypeId = qi.VatTypeId,
                    VatTypeName = qi.VatType != null ? qi.VatType.TypeName : null,
                    VatRate = qi.VatType != null ? qi.VatType.Rate : 0,
                    Discount = qi.Discount != null ? new QuoteItemDiscountDto
                    {
                        QuoteItemDiscountId = qi.Discount.QuoteItemDiscountId,
                        QuoteItemId = qi.Discount.QuoteItemId,
                        DiscountType = qi.Discount.DiscountType,
                        DiscountPercentage = qi.Discount.DiscountPercentage,
                        DiscountAmount = qi.Discount.DiscountAmount,
                        BasePrice = qi.Discount.BasePrice,
                        PartnerPrice = qi.Discount.PartnerPrice,
                        VolumeThreshold = qi.Discount.VolumeThreshold,
                        VolumePrice = qi.Discount.VolumePrice
                    } : null
                })
                .ToListAsync();
        }

        public async Task<QuoteDto> CreateQuoteAsync(CreateQuoteDto quoteDto)
        {
            if (string.IsNullOrEmpty(quoteDto.QuoteNumber))
            {
                quoteDto.QuoteNumber = await GetNextQuoteNumberAsync();
            }

            var quote = new Quote
            {
                QuoteNumber = quoteDto.QuoteNumber,
                PartnerId = quoteDto.PartnerId,
                CurrencyId = quoteDto.CurrencyId,
                QuoteDate = quoteDto.QuoteDate ?? DateTime.UtcNow,
                Status = quoteDto.Status ?? "Draft",
                TotalAmount = 0, // Will be updated after items are added
                SalesPerson = quoteDto.SalesPerson,
                ValidityDate = quoteDto.ValidityDate,
                Subject = quoteDto.Subject,
                Description = quoteDto.Description,
                DetailedDescription = quoteDto.DetailedDescription,
                DiscountPercentage = quoteDto.DiscountPercentage, // Quote-level discount
                DiscountAmount = quoteDto.DiscountAmount, // Quote-level discount
                CompanyName = quoteDto.CompanyName,
                CreatedBy = quoteDto.CreatedBy ?? "System",
                CreatedDate = quoteDto.CreatedDate ?? DateTime.UtcNow,
                ModifiedBy = quoteDto.ModifiedBy ?? "System",
                ModifiedDate = quoteDto.ModifiedDate ?? DateTime.UtcNow,
                ReferenceNumber = quoteDto.ReferenceNumber,
                QuoteItems = new List<QuoteItem>()
            };

            decimal totalAmount = 0;
            foreach (var itemDto in quoteDto.Items)
            {
                var productPrice = await _context.ProductPrices
                    .Where(pp => pp.ProductId == itemDto.ProductId && pp.IsActive)
                    .FirstOrDefaultAsync();
                var partnerPrice = await _context.PartnerProductPrice
                    .Where(pp => pp.ProductId == itemDto.ProductId && pp.PartnerId == quoteDto.PartnerId)
                    .FirstOrDefaultAsync();

                decimal basePrice = productPrice?.SalesPrice ?? 0;
                decimal unitPrice = basePrice;

                QuoteItemDiscount discount = null;
                if (itemDto.Discount != null && itemDto.Discount.DiscountType != DiscountType.NoDiscount)
                {
                    discount = new QuoteItemDiscount
                    {
                        DiscountType = itemDto.Discount.DiscountType,
                        DiscountPercentage = itemDto.Discount.DiscountPercentage,
                        DiscountAmount = itemDto.Discount.DiscountAmount,
                        PartnerPrice = itemDto.Discount.PartnerPrice,
                        VolumeThreshold = itemDto.Discount.VolumeThreshold,
                        VolumePrice = itemDto.Discount.VolumePrice
                    };

                    switch (itemDto.Discount.DiscountType)
                    {
                        case DiscountType.CustomDiscountPercentage:
                            discount.DiscountPercentage = discount.DiscountPercentage ?? 0;
                            unitPrice = basePrice * (1 - discount.DiscountPercentage.Value / 100);
                            discount.DiscountAmount = null;
                            discount.PartnerPrice = null;
                            discount.VolumeThreshold = null;
                            discount.VolumePrice = null;
                            break;
                        case DiscountType.CustomDiscountAmount:
                            discount.DiscountAmount = discount.DiscountAmount ?? 0;
                            unitPrice = (basePrice * itemDto.Quantity - discount.DiscountAmount.Value) / itemDto.Quantity;
                            discount.DiscountPercentage = null;
                            discount.PartnerPrice = null;
                            discount.VolumeThreshold = null;
                            discount.VolumePrice = null;
                            break;
                        case DiscountType.PartnerPrice:
                            basePrice = partnerPrice?.PartnerUnitPrice ?? basePrice;
                            unitPrice = basePrice;
                            discount.PartnerPrice = basePrice;
                            discount.DiscountPercentage = null;
                            discount.DiscountAmount = null;
                            discount.VolumeThreshold = null;
                            discount.VolumePrice = null;
                            break;
                        case DiscountType.VolumeDiscount:
                            if (itemDto.Quantity >= productPrice?.Volume3 && productPrice?.Volume3Price > 0)
                            {
                                basePrice = productPrice.Volume3Price;
                                discount.VolumeThreshold = productPrice.Volume3;
                                discount.VolumePrice = productPrice.Volume3Price;
                            }
                            else if (itemDto.Quantity >= productPrice?.Volume2 && productPrice?.Volume2Price > 0)
                            {
                                basePrice = productPrice.Volume2Price;
                                discount.VolumeThreshold = productPrice.Volume2;
                                discount.VolumePrice = productPrice.Volume2Price;
                            }
                            else if (itemDto.Quantity >= productPrice?.Volume1 && productPrice?.Volume1Price > 0)
                            {
                                basePrice = productPrice.Volume1Price;
                                discount.VolumeThreshold = productPrice.Volume1;
                                discount.VolumePrice = productPrice.Volume1Price;
                            }
                            unitPrice = basePrice;
                            discount.DiscountPercentage = null;
                            discount.DiscountAmount = null;
                            discount.PartnerPrice = null;
                            break;
                    }
                    discount.BasePrice = basePrice;
                }

                var quoteItem = new QuoteItem
                {
                    QuoteId = quote.QuoteId,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice,
                    ItemDescription = itemDto.ItemDescription,
                    VatTypeId = itemDto.VatTypeId,
                    TotalPrice = itemDto.Quantity * unitPrice,
                    Discount = discount
                };

                quote.QuoteItems.Add(quoteItem);
                totalAmount += quoteItem.TotalPrice;
            }

            if (quote.DiscountPercentage.HasValue)
            {
                totalAmount *= (1 - quote.DiscountPercentage.Value / 100);
            }
            else if (quote.DiscountAmount.HasValue)
            {
                totalAmount -= quote.DiscountAmount.Value;
            }
            quote.TotalAmount = totalAmount;

            try
            {
                _context.Quotes.Add(quote);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Quote saved with ID: {QuoteId}", quote.QuoteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving quote");
                throw;
            }

            return await GetQuoteByIdAsync(quote.QuoteId);
        }

        public async Task<List<PartnerDto>> GetPartnersAsync()
        {
            try
            {
                var partners = await _context.Partners
                    .Select(p => new PartnerDto
                    {
                        PartnerId = p.PartnerId,
                        Name = p.Name
                    })
                    .ToListAsync();
                _logger.LogInformation($"Fetched {partners.Count} partners from database.");
                return partners;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partners from database.");
                throw;
            }
        }

        public async Task<QuoteDto> GetQuoteByIdAsync(int quoteId)
        {
            var quote = await _context.Quotes
                .Include(q => q.QuoteItems)
                    .ThenInclude(qi => qi.Product)
                .Include(q => q.QuoteItems)
                    .ThenInclude(qi => qi.Discount)
                .Include(q => q.Currency)
                .Where(q => q.QuoteId == quoteId)
                .Select(q => new QuoteDto
                {
                    QuoteId = q.QuoteId,
                    QuoteNumber = q.QuoteNumber,
                    PartnerId = q.PartnerId,
                    CurrencyId = q.CurrencyId,
                    QuoteDate = q.QuoteDate,
                    Status = q.Status,
                    TotalAmount = q.TotalAmount,
                    SalesPerson = q.SalesPerson,
                    ValidityDate = q.ValidityDate,
                    Subject = q.Subject,
                    Description = q.Description,
                    DetailedDescription = q.DetailedDescription,
                    DiscountPercentage = q.DiscountPercentage,
                    DiscountAmount = q.DiscountAmount,
                    CompanyName = q.CompanyName,
                    CreatedBy = q.CreatedBy,
                    CreatedDate = q.CreatedDate,
                    ModifiedBy = q.ModifiedBy,
                    ModifiedDate = q.ModifiedDate,
                    ReferenceNumber = q.ReferenceNumber,
                    Currency = new CurrencyDto
                    {
                        CurrencyId = q.Currency.CurrencyId,
                        CurrencyName = q.Currency.CurrencyName,
                        ExchangeRate = q.Currency.ExchangeRate,
                        IsBaseCurrency = q.Currency.IsBaseCurrency,
                        CreatedBy = q.Currency.CreatedBy,
                        LastModifiedBy = q.Currency.LastModifiedBy,
                        CreatedAt = q.Currency.CreatedAt,
                        UpdatedAt = q.Currency.UpdatedAt
                    },
                    Items = q.QuoteItems.Select(i => new QuoteItemDto
                    {
                        QuoteItemId = i.QuoteItemId,
                        QuoteId = i.QuoteId,
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        ItemDescription = i.ItemDescription,
                        TotalPrice = i.TotalPrice,
                        VatTypeId = i.VatTypeId,
                        Discount = i.Discount != null ? new QuoteItemDiscountDto
                        {
                            QuoteItemDiscountId = i.Discount.QuoteItemDiscountId,
                            QuoteItemId = i.Discount.QuoteItemId,
                            DiscountType = i.Discount.DiscountType,
                            DiscountPercentage = i.Discount.DiscountPercentage,
                            DiscountAmount = i.Discount.DiscountAmount,
                            BasePrice = i.Discount.BasePrice,
                            PartnerPrice = i.Discount.PartnerPrice,
                            VolumeThreshold = i.Discount.VolumeThreshold,
                            VolumePrice = i.Discount.VolumePrice
                        } : null
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (quote == null)
                return new QuoteDto();

            return quote;
        }

        public async Task<QuoteDto> UpdateQuoteAsync(int quoteId, UpdateQuoteDto quoteDto)
        {
            _logger.LogInformation("UpdateQuoteAsync called for QuoteId: {QuoteId}, QuoteDto: {QuoteDto}", 
                quoteId, JsonSerializer.Serialize(quoteDto));

            if (quoteDto == null)
            {
                _logger.LogWarning("UpdateQuoteAsync received null QuoteDto for QuoteId: {QuoteId}", quoteId);
                throw new ArgumentNullException(nameof(quoteDto));
            }

            var quote = await _context.Quotes
                .Include(q => q.QuoteItems)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);
            if (quote == null)
            {
                _logger.LogWarning("Quote not found for QuoteId: {QuoteId}", quoteId);
                return null;
            }

            quote.QuoteNumber = quoteDto.QuoteNumber;
            quote.PartnerId = quoteDto.PartnerId;
            quote.QuoteDate = quoteDto.QuoteDate;
            quote.CurrencyId = quoteDto.CurrencyId;
            quote.Status = quoteDto.Status;
            quote.TotalAmount = quoteDto.TotalAmount;
            quote.SalesPerson = quoteDto.SalesPerson;
            quote.ValidityDate = quoteDto.ValidityDate;
            quote.Subject = quoteDto.Subject;
            quote.Description = quoteDto.Description;
            quote.DetailedDescription = quoteDto.DetailedDescription;
            quote.DiscountAmount = quoteDto.DiscountAmount;
            quote.DiscountPercentage = quoteDto.DiscountPercentage;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated quote ID: {QuoteId}", quoteId);
            return await GetQuoteByIdAsync(quoteId);
        }

        public async Task<bool> DeleteQuoteAsync(int quoteId)
        {
            var quote = await _context.Quotes
                .Include(q => q.QuoteItems)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (quote == null)
            {
                return false;
            }

            _context.QuoteItems.RemoveRange(quote.QuoteItems);
            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<QuoteItemResponseDto> CreateQuoteItemAsync(int quoteId, CreateQuoteItemDto itemDto)
        {
            _logger.LogInformation("CreateQuoteItemAsync called for QuoteId: {QuoteId}, ItemDto: {ItemDto}",
                quoteId, JsonSerializer.Serialize(itemDto));

            if (itemDto == null)
            {
                _logger.LogWarning("CreateQuoteItemAsync received null ItemDto for QuoteId: {QuoteId}", quoteId);
                throw new ArgumentNullException(nameof(itemDto));
            }

            var quote = await _context.Quotes.FirstOrDefaultAsync(q => q.QuoteId == quoteId);
            if (quote == null)
            {
                _logger.LogWarning("Quote not found for QuoteId: {QuoteId}", quoteId);
                throw new ArgumentException($"Érvénytelen QuoteId: {quoteId}");
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == itemDto.ProductId);
            if (product == null)
            {
                _logger.LogWarning("Product not found for ProductId: {ProductId}", itemDto.ProductId);
                throw new ArgumentException($"Érvénytelen ProductId: {itemDto.ProductId}");
            }

            var vatType = await _context.VatTypes.FirstOrDefaultAsync(v => v.VatTypeId == itemDto.VatTypeId);
            if (vatType == null)
            {
                _logger.LogWarning("Invalid VatTypeId: {VatTypeId}", itemDto.VatTypeId);
                throw new ArgumentException($"Érvénytelen VatTypeId: {itemDto.VatTypeId}");
            }

            var productPrice = await _context.ProductPrices
                .Where(pp => pp.ProductId == itemDto.ProductId && pp.IsActive)
                .FirstOrDefaultAsync();
            var partnerPrice = await _context.PartnerProductPrice
                .Where(pp => pp.ProductId == itemDto.ProductId && pp.PartnerId == quote.PartnerId)
                .FirstOrDefaultAsync();

            decimal basePrice = productPrice?.SalesPrice ?? 0;
            decimal unitPrice = basePrice;

            QuoteItemDiscount discount = null;
            if (itemDto.Discount != null && itemDto.Discount.DiscountType != DiscountType.NoDiscount)
            {
                discount = new QuoteItemDiscount
                {
                    DiscountType = itemDto.Discount.DiscountType,
                    DiscountPercentage = itemDto.Discount.DiscountPercentage,
                    DiscountAmount = itemDto.Discount.DiscountAmount,
                    PartnerPrice = itemDto.Discount.PartnerPrice,
                    VolumeThreshold = itemDto.Discount.VolumeThreshold,
                    VolumePrice = itemDto.Discount.VolumePrice
                };

                switch (itemDto.Discount.DiscountType)
                {
                    case DiscountType.CustomDiscountPercentage:
                        discount.DiscountPercentage = discount.DiscountPercentage ?? 0;
                        unitPrice = basePrice * (1 - discount.DiscountPercentage.Value / 100);
                        discount.DiscountAmount = null;
                        discount.PartnerPrice = null;
                        discount.VolumeThreshold = null;
                        discount.VolumePrice = null;
                        break;
                    case DiscountType.CustomDiscountAmount:
                        discount.DiscountAmount = discount.DiscountAmount ?? 0;
                        unitPrice = (basePrice * itemDto.Quantity - discount.DiscountAmount.Value) / itemDto.Quantity;
                        discount.DiscountPercentage = null;
                        discount.PartnerPrice = null;
                        discount.VolumeThreshold = null;
                        discount.VolumePrice = null;
                        break;
                    case DiscountType.PartnerPrice:
                        basePrice = partnerPrice?.PartnerUnitPrice ?? basePrice;
                        unitPrice = basePrice;
                        discount.PartnerPrice = basePrice;
                        discount.DiscountPercentage = null;
                        discount.DiscountAmount = null;
                        discount.VolumeThreshold = null;
                        discount.VolumePrice = null;
                        break;
                    case DiscountType.VolumeDiscount:
                        if (itemDto.Quantity >= productPrice?.Volume3 && productPrice?.Volume3Price > 0)
                        {
                            basePrice = productPrice.Volume3Price;
                            discount.VolumeThreshold = productPrice.Volume3;
                            discount.VolumePrice = productPrice.Volume3Price;
                        }
                        else if (itemDto.Quantity >= productPrice?.Volume2 && productPrice?.Volume2Price > 0)
                        {
                            basePrice = productPrice.Volume2Price;
                            discount.VolumeThreshold = productPrice.Volume2;
                            discount.VolumePrice = productPrice.Volume2Price;
                        }
                        else if (itemDto.Quantity >= productPrice?.Volume1 && productPrice?.Volume1Price > 0)
                        {
                            basePrice = productPrice.Volume1Price;
                            discount.VolumeThreshold = productPrice.Volume1;
                            discount.VolumePrice = productPrice.Volume1Price;
                        }
                        unitPrice = basePrice;
                        discount.DiscountPercentage = null;
                        discount.DiscountAmount = null;
                        discount.PartnerPrice = null;
                        break;
                }
                discount.BasePrice = basePrice;
            }

            var quoteItem = new QuoteItem
            {
                QuoteId = quoteId,
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                UnitPrice = unitPrice,
                ItemDescription = itemDto.ItemDescription ?? "",
                VatTypeId = itemDto.VatTypeId,
                TotalPrice = itemDto.Quantity * unitPrice,
                Discount = discount
            };

            _context.QuoteItems.Add(quoteItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created quote item ID: {QuoteItemId} for QuoteId: {QuoteId}",
                quoteItem.QuoteItemId, quoteId);

            return new QuoteItemResponseDto
            {
                QuoteItemId = quoteItem.QuoteItemId,
                QuoteId = quoteItem.QuoteId,
                ProductId = quoteItem.ProductId,
                Quantity = quoteItem.Quantity,
                UnitPrice = quoteItem.UnitPrice,
                ItemDescription = quoteItem.ItemDescription,
                TotalPrice = quoteItem.TotalPrice,
                VatTypeId = quoteItem.VatTypeId,
                Discount = discount != null ? new QuoteItemDiscountDto
                {
                    QuoteItemDiscountId = discount.QuoteItemDiscountId,
                    QuoteItemId = discount.QuoteItemId,
                    DiscountType = discount.DiscountType,
                    DiscountPercentage = discount.DiscountPercentage,
                    DiscountAmount = discount.DiscountAmount,
                    BasePrice = discount.BasePrice,
                    PartnerPrice = discount.PartnerPrice,
                    VolumeThreshold = discount.VolumeThreshold,
                    VolumePrice = discount.VolumePrice
                } : null
            };
        }

        public async Task<QuoteItemResponseDto> UpdateQuoteItemAsync(int quoteId, int quoteItemId, UpdateQuoteItemDto itemDto)
        {
            _logger.LogInformation("UpdateQuoteItemAsync called for QuoteId: {QuoteId}, QuoteItemId: {QuoteItemId}, ItemDto: {ItemDto}", 
                quoteId, quoteItemId, JsonSerializer.Serialize(itemDto));

            if (itemDto == null)
            {
                _logger.LogWarning("UpdateQuoteItemAsync received null ItemDto for QuoteId: {QuoteId}", quoteId);
                throw new ArgumentNullException(nameof(itemDto));
            }

            var quoteItem = await _context.QuoteItems
                .Include(qi => qi.Discount)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId && q.QuoteItemId == quoteItemId);
            if (quoteItem == null)
            {
                _logger.LogWarning("Quote item not found for QuoteId: {QuoteId}, QuoteItemId: {QuoteItemId}", quoteId, quoteItemId);
                return null;
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == itemDto.ProductId);
            if (product == null)
            {
                _logger.LogWarning("Product not found for ProductId: {ProductId}", itemDto.ProductId);
                throw new ArgumentException($"Érvénytelen ProductId: {itemDto.ProductId}");
            }

            var vatType = await _context.VatTypes.FirstOrDefaultAsync(v => v.VatTypeId == itemDto.VatTypeId);
            if (vatType == null)
            {
                _logger.LogWarning("Invalid VatTypeId: {VatTypeId}", itemDto.VatTypeId);
                throw new ArgumentException($"Érvénytelen VatTypeId: {itemDto.VatTypeId}");
            }

            var productPrice = await _context.ProductPrices
                .Where(pp => pp.ProductId == itemDto.ProductId && pp.IsActive)
                .FirstOrDefaultAsync();
            var partnerPrice = await _context.PartnerProductPrice
                .Where(pp => pp.ProductId == itemDto.ProductId && pp.PartnerId == itemDto.QuoteId) // Assuming QuoteId links to Partner
                .FirstOrDefaultAsync();

            decimal basePrice = productPrice?.SalesPrice ?? 0;
            decimal unitPrice = basePrice;

            if (quoteItem.Discount != null)
            {
                _context.QuoteItemDiscounts.Remove(quoteItem.Discount);
            }

            QuoteItemDiscount discount = null;
            if (itemDto.Discount != null && itemDto.Discount.DiscountType != DiscountType.NoDiscount)
            {
                discount = new QuoteItemDiscount
                {
                    QuoteItemId = quoteItem.QuoteItemId,
                    DiscountType = itemDto.Discount.DiscountType,
                    DiscountPercentage = itemDto.Discount.DiscountPercentage,
                    DiscountAmount = itemDto.Discount.DiscountAmount,
                    PartnerPrice = itemDto.Discount.PartnerPrice,
                    VolumeThreshold = itemDto.Discount.VolumeThreshold,
                    VolumePrice = itemDto.Discount.VolumePrice
                };

                switch (itemDto.Discount.DiscountType)
                {
                    case DiscountType.CustomDiscountPercentage:
                        discount.DiscountPercentage = discount.DiscountPercentage ?? 0;
                        unitPrice = basePrice * (1 - discount.DiscountPercentage.Value / 100);
                        discount.DiscountAmount = null;
                        discount.PartnerPrice = null;
                        discount.VolumeThreshold = null;
                        discount.VolumePrice = null;
                        break;
                    case DiscountType.CustomDiscountAmount:
                        discount.DiscountAmount = discount.DiscountAmount ?? 0;
                        unitPrice = (basePrice * itemDto.Quantity - discount.DiscountAmount.Value) / itemDto.Quantity;
                        discount.DiscountPercentage = null;
                        discount.PartnerPrice = null;
                        discount.VolumeThreshold = null;
                        discount.VolumePrice = null;
                        break;
                    case DiscountType.PartnerPrice:
                        basePrice = partnerPrice?.PartnerUnitPrice ?? basePrice;
                        unitPrice = basePrice;
                        discount.PartnerPrice = basePrice;
                        discount.DiscountPercentage = null;
                        discount.DiscountAmount = null;
                        discount.VolumeThreshold = null;
                        discount.VolumePrice = null;
                        break;
                    case DiscountType.VolumeDiscount:
                        if (itemDto.Quantity >= productPrice?.Volume3 && productPrice?.Volume3Price > 0)
                        {
                            basePrice = productPrice.Volume3Price;
                            discount.VolumeThreshold = productPrice.Volume3;
                            discount.VolumePrice = productPrice.Volume3Price;
                        }
                        else if (itemDto.Quantity >= productPrice?.Volume2 && productPrice?.Volume2Price > 0)
                        {
                            basePrice = productPrice.Volume2Price;
                            discount.VolumeThreshold = productPrice.Volume2;
                            discount.VolumePrice = productPrice.Volume2Price;
                        }
                        else if (itemDto.Quantity >= productPrice?.Volume1 && productPrice?.Volume1Price > 0)
                        {
                            basePrice = productPrice.Volume1Price;
                            discount.VolumeThreshold = productPrice.Volume1;
                            discount.VolumePrice = productPrice.Volume1Price;
                        }
                        unitPrice = basePrice;
                        discount.DiscountPercentage = null;
                        discount.DiscountAmount = null;
                        discount.PartnerPrice = null;
                        break;
                }
                discount.BasePrice = basePrice;
            }

            quoteItem.ProductId = itemDto.ProductId;
            quoteItem.Quantity = itemDto.Quantity;
            quoteItem.UnitPrice = unitPrice;
            quoteItem.ItemDescription = itemDto.ItemDescription;
            quoteItem.VatTypeId = itemDto.VatTypeId;
            quoteItem.TotalPrice = itemDto.Quantity * unitPrice;
            quoteItem.Discount = discount;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated quote item ID: {QuoteItemId} for QuoteId: {QuoteId}", quoteItemId, quoteId);
            return new QuoteItemResponseDto
            {
                QuoteItemId = quoteItem.QuoteItemId,
                QuoteId = quoteItem.QuoteId,
                ProductId = quoteItem.ProductId,
                Quantity = quoteItem.Quantity,
                UnitPrice = quoteItem.UnitPrice,
                ItemDescription = quoteItem.ItemDescription,
                TotalPrice = quoteItem.TotalPrice,
                VatTypeId = quoteItem.VatTypeId,
                Discount = discount != null ? new QuoteItemDiscountDto
                {
                    QuoteItemDiscountId = discount.QuoteItemDiscountId,
                    QuoteItemId = discount.QuoteItemId,
                    DiscountType = discount.DiscountType,
                    DiscountPercentage = discount.DiscountPercentage,
                    DiscountAmount = discount.DiscountAmount,
                    BasePrice = discount.BasePrice,
                    PartnerPrice = discount.PartnerPrice,
                    VolumeThreshold = discount.VolumeThreshold,
                    VolumePrice = discount.VolumePrice
                } : null
            };
        }

        public async Task<bool> DeleteQuoteItemAsync(int quoteId, int quoteItemId)
        {
            var item = await _context.QuoteItems
                .FirstOrDefaultAsync(qi => qi.QuoteId == quoteId && qi.QuoteItemId == quoteItemId);

            if (item == null)
            {
                return false;
            }

            _context.QuoteItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<QuoteDto> CopyQuoteAsync(int quoteId)
        {
            var originalQuote = await _context.Quotes
                .Include(q => q.QuoteItems)
                    .ThenInclude(qi => qi.Discount)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (originalQuote == null)
            {
                throw new KeyNotFoundException($"Quote with ID {quoteId} not found");
            }

            var newQuote = new Quote
            {
                QuoteNumber = await GetNextQuoteNumberAsync(),
                PartnerId = originalQuote.PartnerId,
                CurrencyId = originalQuote.CurrencyId,
                QuoteDate = DateTime.UtcNow,
                Status = "Draft",
                TotalAmount = originalQuote.TotalAmount,
                SalesPerson = originalQuote.SalesPerson,
                ValidityDate = originalQuote.ValidityDate,
                Subject = originalQuote.Subject,
                Description = originalQuote.Description,
                DetailedDescription = originalQuote.DetailedDescription,
                DiscountPercentage = originalQuote.DiscountPercentage,
                DiscountAmount = originalQuote.DiscountAmount,
                CompanyName = originalQuote.CompanyName,
                CreatedBy = originalQuote.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedBy = originalQuote.ModifiedBy,
                ModifiedDate = DateTime.UtcNow,
                ReferenceNumber = originalQuote.ReferenceNumber,
                QuoteItems = originalQuote.QuoteItems.Select(qi => new QuoteItem
                {
                    ProductId = qi.ProductId,
                    Quantity = qi.Quantity,
                    UnitPrice = qi.UnitPrice,
                    ItemDescription = qi.ItemDescription,
                    VatTypeId = qi.VatTypeId,
                    TotalPrice = qi.TotalPrice,
                    Discount = qi.Discount != null ? new QuoteItemDiscount
                    {
                        DiscountType = qi.Discount.DiscountType,
                        DiscountPercentage = qi.Discount.DiscountPercentage,
                        DiscountAmount = qi.Discount.DiscountAmount,
                        BasePrice = qi.Discount.BasePrice,
                        PartnerPrice = qi.Discount.PartnerPrice,
                        VolumeThreshold = qi.Discount.VolumeThreshold,
                        VolumePrice = qi.Discount.VolumePrice
                    } : null
                }).ToList()
            };

            _context.Quotes.Add(newQuote);
            await _context.SaveChangesAsync();

            return await GetQuoteByIdAsync(newQuote.QuoteId);
        }

        public async Task<OrderDto> ConvertQuoteToOrderAsync(int quoteId, ConvertQuoteToOrderDto convertDto, string createdBy)
        {
            _logger.LogInformation("Converting quote ID: {QuoteId} to order", quoteId);

            var quote = await _context.Quotes
                .Include(q => q.QuoteItems)
                    .ThenInclude(qi => qi.Discount)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);
            if (quote == null)
            {
                _logger.LogWarning("Quote not found for QuoteId: {QuoteId}", quoteId);
                throw new KeyNotFoundException($"Quote with ID {quoteId} not found");
            }

            if (!quote.QuoteItems.Any())
            {
                _logger.LogWarning("Quote ID: {QuoteId} has no items", quoteId);
                throw new InvalidOperationException("Quote must have at least one item to convert to an order");
            }

            if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == convertDto.CurrencyId))
            {
                _logger.LogWarning("Invalid CurrencyId: {CurrencyId}", convertDto.CurrencyId);
                throw new ArgumentException($"Invalid CurrencyId: {convertDto.CurrencyId}");
            }

            if (convertDto.SiteId.HasValue && !await _context.Sites.AnyAsync(s => s.SiteId == convertDto.SiteId.Value))
            {
                _logger.LogWarning("Invalid SiteId: {SiteId}", convertDto.SiteId);
                throw new ArgumentException($"Invalid SiteId: {convertDto.SiteId}");
            }

            var createOrderDto = new CreateOrderDto
            {
                PartnerId = quote.PartnerId,
                CurrencyId = convertDto.CurrencyId,
                SiteId = convertDto.SiteId,
                QuoteId = quote.QuoteId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = quote.TotalAmount,
                DiscountPercentage = quote.DiscountPercentage,
                DiscountAmount = quote.DiscountAmount,
                SalesPerson = quote.SalesPerson,
                Subject = quote.Subject,
                Description = quote.Description,
                DetailedDescription = quote.DetailedDescription,
                Status = "Draft",
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                PaymentTerms = convertDto.PaymentTerms,
                ShippingMethod = convertDto.ShippingMethod,
                OrderType = convertDto.OrderType,
                OrderItems = quote.QuoteItems.Select(qi => new CreateOrderItemDto
                {
                    ProductId = qi.ProductId,
                    Quantity = qi.Quantity,
                    UnitPrice = qi.UnitPrice,
                    Description = qi.ItemDescription,
                    // Map discount to OrderItem discount (assuming similar structure)
                    // Discount = qi.Discount != null ? new OrderItemDiscountDto
                    // {
                    //     DiscountType = qi.Discount.DiscountType.ToString(), // Adjust if OrderItemDiscountDto uses different enum
                    //     DiscountPercentage = qi.Discount.DiscountPercentage,
                    //     DiscountAmount = qi.Discount.DiscountAmount,
                    //     BasePrice = qi.Discount.BasePrice,
                    //     PartnerPrice = qi.Discount.PartnerPrice,
                    //     VolumeThreshold = qi.Discount.VolumeThreshold,
                    //     VolumePrice = qi.Discount.VolumePrice
                    // } : null
                }).ToList()
            };

            var orderDto = await _orderService.CreateOrderAsync(createOrderDto);

            quote.Status = "Converted";
            await _context.SaveChangesAsync();

            var fullOrderDto = await _orderService.GetOrderByIdAsync(orderDto.OrderId);
            if (fullOrderDto == null)
            {
                _logger.LogError("Failed to retrieve created order ID: {OrderId}", orderDto.OrderId);
                throw new InvalidOperationException("Failed to retrieve created order");
            }

            _logger.LogInformation("Converted quote ID: {QuoteId} to order ID: {OrderId}", quoteId, orderDto.OrderId);
            return fullOrderDto;
        }
    }
}