using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

namespace Cloud9_2.Services
{
    
public interface IQuoteService
    {
        Task<string> GetNextQuoteNumberAsync();
        Task<bool> QuoteExistsAsync(int quoteId);
        Task<List<PartnerDto>> GetPartnersAsync();
        Task<List<QuoteItemDto>> GetQuoteItemsAsync(int quoteId);
        Task<QuoteDto> CreateQuoteAsync(CreateQuoteDto quoteDto);
        Task<QuoteDto> GetQuoteByIdAsync(int quoteId);
        Task<QuoteDto> UpdateQuoteAsync(int quoteId, UpdateQuoteDto quoteDto);
        Task<bool> DeleteQuoteAsync(int quoteId);
        Task<QuoteItemResponseDto> CreateQuoteItemAsync(int quoteId, CreateQuoteItemDto itemDto);
        Task<QuoteItemResponseDto> UpdateQuoteItemAsync(int quoteId, int quoteItemId, UpdateQuoteItemDto itemDto);
        Task<bool> DeleteQuoteItemAsync(int quoteId, int quoteItemId);
        Task<QuoteDto> CopyQuoteAsync(int quoteId);
        // Task<OrderDto> ConvertQuoteToOrderAsync(int quoteId, ConvertQuoteToOrderDto convertDto, string createdBy);
    }

public class QuoteService : IQuoteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuoteService> _logger;
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;

        public QuoteService(ApplicationDbContext context, IMapper mapper, ILogger<QuoteService> logger, IOrderService orderService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _mapper = mapper;
        }

        private static readonly SemaphoreSlim _quoteNumberLock = new SemaphoreSlim(1, 1);

        public Task<string> GetNextQuoteNumberAsync()
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHfff");
            return Task.FromResult($"QUOTE-{timestamp}");
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
                .Select(qi => new QuoteItemDto
                {
                    QuoteItemId = qi.QuoteItemId,
                    QuoteId = qi.QuoteId,
                    ProductId = qi.ProductId,
                    Quantity = qi.Quantity,
                    NetDiscountedPrice = qi.NetDiscountedPrice,
                    ItemDescription = qi.ItemDescription,
                    TotalPrice = qi.TotalPrice,
                    VatTypeId = qi.VatTypeId,
                    DiscountTypeId = qi.DiscountTypeId,
                    DiscountAmount = qi.DiscountAmount
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
                TotalAmount = 0,
                SalesPerson = quoteDto.SalesPerson,
                ValidityDate = quoteDto.ValidityDate,
                Subject = quoteDto.Subject,
                Description = quoteDto.Description,
                DetailedDescription = quoteDto.DetailedDescription,
                DiscountPercentage = quoteDto.DiscountPercentage,
                QuoteDiscountAmount = quoteDto.DiscountAmount,
                TotalItemDiscounts = quoteDto.TotalItemDiscounts ?? 0,
                CompanyName = quoteDto.CompanyName,
                CreatedBy = quoteDto.CreatedBy ?? "System",
                CreatedDate = quoteDto.CreatedDate ?? DateTime.UtcNow,
                ModifiedBy = quoteDto.ModifiedBy ?? "System",
                ModifiedDate = quoteDto.ModifiedDate ?? DateTime.UtcNow,
                ReferenceNumber = quoteDto.ReferenceNumber,
                QuoteItems = new List<QuoteItem>()
            };

            if (quoteDto.Items == null || !quoteDto.Items.Any())
            {
                _logger.LogWarning("No items provided for quote creation");
                throw new ArgumentException("A quote must contain at least one item");
            }

            decimal totalItemDiscounts = 0;
            decimal totalAmount = 0;

            foreach (var itemDto in quoteDto.Items)
            {
                // Validate and normalize DiscountAmount for NoDiscount
                if (itemDto.DiscountTypeId == (int)DiscountType.NoDiscount)
                {
                    if (itemDto.DiscountAmount != null)
                    {
                        _logger.LogWarning("DiscountAmount {DiscountAmount} provided for NoDiscount (DiscountTypeId: {DiscountTypeId}) for ProductId: {ProductId}. Setting to null.",
                            itemDto.DiscountAmount, itemDto.DiscountTypeId, itemDto.ProductId);
                        itemDto.DiscountAmount = null;
                    }
                }
                else if (itemDto.DiscountAmount == null)
                {
                    _logger.LogWarning("DiscountAmount is null for DiscountTypeId: {DiscountTypeId} for ProductId: {ProductId}", itemDto.DiscountTypeId, itemDto.ProductId);
                    throw new ArgumentException($"DiscountAmount must be provided for DiscountTypeId: {itemDto.DiscountTypeId} for ProductId: {itemDto.ProductId}");
                }

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == itemDto.ProductId);
                if (product == null)
                {
                    _logger.LogWarning("Invalid ProductId: {ProductId} for QuoteItem", itemDto.ProductId);
                    throw new ArgumentException($"Érvénytelen ProductId: {itemDto.ProductId}");
                }

                var vatType = await _context.VatTypes
                    .FirstOrDefaultAsync(v => v.VatTypeId == itemDto.VatTypeId);
                if (vatType == null)
                {
                    _logger.LogWarning("Invalid VatTypeId: {VatTypeId} for QuoteItem", itemDto.VatTypeId);
                    throw new ArgumentException($"Érvénytelen VatTypeId: {itemDto.VatTypeId}");
                }

                var productPrice = await _context.ProductPrices
                    .Where(pp => pp.ProductId == itemDto.ProductId && pp.IsActive)
                    .FirstOrDefaultAsync();
                var partnerPrice = await _context.PartnerProductPrice
                    .Where(pp => pp.ProductId == itemDto.ProductId && pp.PartnerId == quoteDto.PartnerId)
                    .FirstOrDefaultAsync();

                decimal basePrice = productPrice?.SalesPrice ?? throw new InvalidOperationException($"No active price found for ProductId: {itemDto.ProductId}");
                decimal netDiscountedPrice = itemDto.NetDiscountedPrice > 0 ? itemDto.NetDiscountedPrice : basePrice;
                decimal itemDiscount = 0;

                if (itemDto.DiscountTypeId.HasValue && itemDto.DiscountTypeId != (int)DiscountType.NoDiscount)
                {
                    _logger.LogInformation("Processing discount for item {ProductId}: Type={DiscountTypeId}", itemDto.ProductId, itemDto.DiscountTypeId);
                    switch (itemDto.DiscountTypeId)
                    {
                        case (int)DiscountType.CustomDiscountPercentage:
                            if (itemDto.DiscountAmount < 0)
                            {
                                _logger.LogWarning("Invalid DiscountAmount: {DiscountAmount} for ProductId: {ProductId}", itemDto.DiscountAmount, itemDto.ProductId);
                                throw new ArgumentException($"DiscountAmount must be non-negative for ProductId: {itemDto.ProductId}");
                            }
                            itemDiscount = itemDto.DiscountAmount.Value * itemDto.Quantity;
                            netDiscountedPrice = basePrice * (1 - itemDto.DiscountAmount.Value / 100);
                            break;
                        case (int)DiscountType.CustomDiscountAmount:
                            if (itemDto.DiscountAmount < 0)
                            {
                                _logger.LogWarning("Invalid DiscountAmount: {DiscountAmount} for ProductId: {ProductId}", itemDto.DiscountAmount, itemDto.ProductId);
                                throw new ArgumentException($"DiscountAmount must be non-negative for ProductId: {itemDto.ProductId}");
                            }
                            itemDiscount = itemDto.DiscountAmount.Value * itemDto.Quantity;
                            netDiscountedPrice = basePrice - itemDto.DiscountAmount.Value;
                            break;
                        case (int)DiscountType.PartnerPrice:
                            if (itemDto.DiscountAmount < 0)
                            {
                                _logger.LogWarning("Invalid DiscountAmount: {DiscountAmount} for ProductId: {ProductId}", itemDto.DiscountAmount, itemDto.ProductId);
                                throw new ArgumentException($"DiscountAmount must be non-negative for ProductId: {itemDto.ProductId}");
                            }
                            itemDiscount = itemDto.DiscountAmount.Value * itemDto.Quantity;
                            netDiscountedPrice = basePrice - itemDto.DiscountAmount.Value;
                            break;
                        case (int)DiscountType.VolumeDiscount:
                            if (itemDto.DiscountAmount < 0)
                            {
                                _logger.LogWarning("Invalid DiscountAmount: {DiscountAmount} for ProductId: {ProductId}", itemDto.DiscountAmount, itemDto.ProductId);
                                throw new ArgumentException($"DiscountAmount must be non-negative for ProductId: {itemDto.ProductId}");
                            }
                            itemDiscount = itemDto.DiscountAmount.Value * itemDto.Quantity;
                            netDiscountedPrice = basePrice - itemDto.DiscountAmount.Value;
                            break;
                        default:
                            _logger.LogWarning("Unknown DiscountTypeId: {DiscountTypeId} for ProductId: {ProductId}", itemDto.DiscountTypeId, itemDto.ProductId);
                            throw new ArgumentException($"Unknown DiscountTypeId: {itemDto.DiscountTypeId}");
                    }
                    netDiscountedPrice = Math.Max(0, netDiscountedPrice);
                }

                var quoteItem = new QuoteItem
                {
                    QuoteId = quote.QuoteId,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    NetDiscountedPrice = netDiscountedPrice,
                    ItemDescription = itemDto.ItemDescription,
                    VatTypeId = itemDto.VatTypeId,
                    TotalPrice = itemDto.Quantity * netDiscountedPrice,
                    DiscountTypeId = itemDto.DiscountTypeId,
                    DiscountAmount = itemDto.DiscountAmount,
                    ListPrice = itemDto.ListPrice ?? basePrice,
                    PartnerPrice = itemDto.PartnerPrice ?? partnerPrice?.PartnerUnitPrice,
                    VolumePrice = itemDto.VolumePrice
                };

                quote.QuoteItems.Add(quoteItem);
                totalItemDiscounts += itemDiscount;
                totalAmount += quoteItem.TotalPrice;
            }

            quote.TotalItemDiscounts = totalItemDiscounts;

            if (quote.DiscountPercentage.HasValue && quote.QuoteDiscountAmount.HasValue)
            {
                _logger.LogWarning("Both DiscountPercentage and QuoteDiscountAmount provided for quote; using DiscountPercentage");
            }
            if (quote.DiscountPercentage.HasValue)
            {
                if (quote.DiscountPercentage.Value < 0 || quote.DiscountPercentage.Value > 100)
                {
                    _logger.LogWarning("Invalid DiscountPercentage: {DiscountPercentage}", quote.DiscountPercentage.Value);
                    throw new ArgumentException("DiscountPercentage must be between 0 and 100");
                }
                totalAmount *= (1 - quote.DiscountPercentage.Value / 100);
            }
            else if (quote.QuoteDiscountAmount.HasValue)
            {
                if (quote.QuoteDiscountAmount.Value < 0)
                {
                    _logger.LogWarning("Invalid QuoteDiscountAmount: {QuoteDiscountAmount}", quote.QuoteDiscountAmount.Value);
                    throw new ArgumentException("QuoteDiscountAmount must be non-negative");
                }
                if (quote.QuoteDiscountAmount.Value > totalAmount)
                {
                    _logger.LogWarning("QuoteDiscountAmount: {QuoteDiscountAmount} exceeds TotalAmount: {TotalAmount}", quote.QuoteDiscountAmount.Value, totalAmount);
                    throw new ArgumentException("QuoteDiscountAmount cannot exceed total amount");
                }
                totalAmount -= quote.QuoteDiscountAmount.Value;
            }
            quote.TotalAmount = Math.Max(0, totalAmount);

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
                    .ThenInclude(qi => qi.VatType)
                .Include(q => q.Currency)
                .Include(q => q.Partner)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (quote == null)
            {
                _logger.LogWarning("Quote not found for QuoteId: {QuoteId}", quoteId);
                return new QuoteDto();
            }

            var quoteDto = new QuoteDto
            {
                QuoteId = quote.QuoteId,
                QuoteNumber = quote.QuoteNumber,
                PartnerId = quote.PartnerId,
                CurrencyId = quote.CurrencyId,
                Currency = new CurrencyDto
                {
                    CurrencyId = quote.Currency.CurrencyId,
                    CurrencyName = quote.Currency.CurrencyName
                },
                Partner = new PartnerDto
                {
                    PartnerId = quote.Partner.PartnerId,
                    Name = quote.Partner.Name
                },
                QuoteDate = quote.QuoteDate,
                Status = quote.Status,
                TotalAmount = quote.TotalAmount,
                SalesPerson = quote.SalesPerson,
                ValidityDate = quote.ValidityDate,
                Subject = quote.Subject,
                Description = quote.Description,
                DetailedDescription = quote.DetailedDescription,
                DiscountPercentage = quote.DiscountPercentage,
                QuoteDiscountAmount = quote.QuoteDiscountAmount,
                TotalItemDiscounts = quote.TotalItemDiscounts,
                CompanyName = quote.CompanyName,
                CreatedBy = quote.CreatedBy,
                CreatedDate = quote.CreatedDate,
                ModifiedBy = quote.ModifiedBy,
                ModifiedDate = quote.ModifiedDate,
                ReferenceNumber = quote.ReferenceNumber,
                Items = quote.QuoteItems.Select(i => new QuoteItemDto
                {
                    QuoteItemId = i.QuoteItemId,
                    QuoteId = i.QuoteId,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    NetDiscountedPrice = i.NetDiscountedPrice,
                    ItemDescription = i.ItemDescription,
                    TotalPrice = i.TotalPrice,
                    VatTypeId = i.VatTypeId,
                    VatType = i.VatType != null
                        ? new VatTypeDto
                        {
                            VatTypeId = i.VatType.VatTypeId,
                            Rate = i.VatType.Rate,
                            TypeName = i.VatType.TypeName,
                            FormattedRate = $"{i.VatType.Rate}%"
                        }
                        : null,
                    ListPrice = i.ListPrice,
                    PartnerPrice = i.PartnerPrice,
                    VolumePrice = i.VolumePrice,
                    DiscountTypeId = i.DiscountTypeId,
                    DiscountAmount = i.DiscountAmount
                }).ToList()
            };

            return quoteDto;
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
            quote.QuoteDiscountAmount = quoteDto.DiscountAmount;
            quote.DiscountPercentage = quoteDto.DiscountPercentage;
            quote.TotalItemDiscounts = quoteDto.TotalItemDiscounts;
            quote.CompanyName = quoteDto.CompanyName;
            quote.ReferenceNumber = quoteDto.ReferenceNumber;

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

            decimal basePrice = productPrice?.SalesPrice ?? throw new InvalidOperationException($"No active price found for ProductId: {itemDto.ProductId}");
            decimal netDiscountedPrice = itemDto.NetDiscountedPrice > 0 ? itemDto.NetDiscountedPrice : basePrice;
            decimal itemDiscount = 0;

            if (itemDto.DiscountTypeId.HasValue && itemDto.DiscountTypeId != (int)DiscountType.NoDiscount)
            {
                if (itemDto.DiscountAmount < 0)
                {
                    _logger.LogWarning("Invalid DiscountAmount: {DiscountAmount} for ProductId: {ProductId}", itemDto.DiscountAmount, itemDto.ProductId);
                    throw new ArgumentException($"DiscountAmount must be non-negative for ProductId: {itemDto.ProductId}");
                }
                itemDiscount = itemDto.DiscountAmount.Value * itemDto.Quantity;
                netDiscountedPrice = basePrice - itemDto.DiscountAmount.Value;
                netDiscountedPrice = Math.Max(0, netDiscountedPrice);
            }

            var quoteItem = new QuoteItem
            {
                QuoteId = quoteId,
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                NetDiscountedPrice = netDiscountedPrice,
                ItemDescription = itemDto.ItemDescription ?? "",
                VatTypeId = itemDto.VatTypeId,
                TotalPrice = itemDto.Quantity * netDiscountedPrice,
                DiscountTypeId = itemDto.DiscountTypeId,
                DiscountAmount = itemDto.DiscountAmount
            };

            _context.QuoteItems.Add(quoteItem);

            quote.TotalItemDiscounts += itemDiscount;
            quote.TotalAmount = quote.QuoteItems.Sum(qi => qi.TotalPrice) + quoteItem.TotalPrice - quote.TotalItemDiscounts - (quote.QuoteDiscountAmount ?? 0);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created quote item ID: {QuoteItemId} for QuoteId: {QuoteId}",
                quoteItem.QuoteItemId, quoteId);

            return new QuoteItemResponseDto
            {
                QuoteItemId = quoteItem.QuoteItemId,
                QuoteId = quoteItem.QuoteId,
                ProductId = quoteItem.ProductId,
                Quantity = quoteItem.Quantity,
                NetDiscountedPrice = quoteItem.NetDiscountedPrice,
                ItemDescription = quoteItem.ItemDescription,
                TotalPrice = quoteItem.TotalPrice,
                VatTypeId = quoteItem.VatTypeId,
                DiscountTypeId = quoteItem.DiscountTypeId,
                DiscountAmount = quoteItem.DiscountAmount
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

            var quote = await _context.Quotes
                .Include(q => q.QuoteItems)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);
            if (quote == null)
            {
                _logger.LogWarning("Quote not found for QuoteId: {QuoteId}", quoteId);
                throw new ArgumentException($"Érvénytelen QuoteId: {quoteId}");
            }

            var quoteItem = await _context.QuoteItems
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
                .Where(pp => pp.ProductId == itemDto.ProductId && pp.PartnerId == quote.PartnerId)
                .FirstOrDefaultAsync();

            decimal basePrice = productPrice?.SalesPrice ?? throw new InvalidOperationException($"No active price found for ProductId: {itemDto.ProductId}");
            decimal netDiscountedPrice = itemDto.NetDiscountedPrice > 0 ? itemDto.NetDiscountedPrice : basePrice;
            decimal oldItemDiscount = quoteItem.DiscountTypeId.HasValue ? (quoteItem.DiscountAmount ?? 0) * quoteItem.Quantity : 0;
            decimal newItemDiscount = 0;

            if (itemDto.DiscountTypeId.HasValue && itemDto.DiscountTypeId != (int)DiscountType.NoDiscount)
            {
                if (itemDto.DiscountAmount < 0)
                {
                    _logger.LogWarning("Invalid DiscountAmount: {DiscountAmount} for ProductId: {ProductId}", itemDto.DiscountAmount, itemDto.ProductId);
                    throw new ArgumentException($"DiscountAmount must be non-negative for ProductId: {itemDto.ProductId}");
                }
                newItemDiscount = itemDto.DiscountAmount.Value * itemDto.Quantity;
                netDiscountedPrice = basePrice - itemDto.DiscountAmount.Value;
                netDiscountedPrice = Math.Max(0, netDiscountedPrice);
            }

            quoteItem.ProductId = itemDto.ProductId;
            quoteItem.Quantity = itemDto.Quantity;
            quoteItem.NetDiscountedPrice = netDiscountedPrice;
            quoteItem.ItemDescription = itemDto.ItemDescription;
            quoteItem.VatTypeId = itemDto.VatTypeId;
            quoteItem.VatType = vatType;
            quoteItem.TotalPrice = itemDto.Quantity * netDiscountedPrice;
            quoteItem.DiscountTypeId = itemDto.DiscountTypeId;
            quoteItem.DiscountAmount = itemDto.DiscountAmount;

            quote.TotalItemDiscounts = quote.TotalItemDiscounts - oldItemDiscount + newItemDiscount;
            quote.TotalAmount = quote.QuoteItems.Sum(qi => qi.TotalPrice) - quote.TotalItemDiscounts - (quote.QuoteDiscountAmount ?? 0);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated quote item ID: {QuoteItemId} for QuoteId: {QuoteId}", quoteItemId, quoteId);
            return new QuoteItemResponseDto
            {
                QuoteItemId = quoteItem.QuoteItemId,
                QuoteId = quoteItem.QuoteId,
                ProductId = quoteItem.ProductId,
                Quantity = quoteItem.Quantity,
                NetDiscountedPrice = quoteItem.NetDiscountedPrice,
                ItemDescription = quoteItem.ItemDescription,
                TotalPrice = quoteItem.TotalPrice,
                VatTypeId = quoteItem.VatTypeId,
                VatType = new VatTypeDto
                {
                    VatTypeId = vatType.VatTypeId,
                    Rate = vatType.Rate,
                    TypeName = vatType.TypeName
                },
                DiscountTypeId = quoteItem.DiscountTypeId,
                DiscountAmount = quoteItem.DiscountAmount
            };
        }

        public async Task<bool> DeleteQuoteItemAsync(int quoteId, int quoteItemId)
        {
            var quote = await _context.Quotes
                .Include(q => q.QuoteItems)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);
            if (quote == null)
            {
                return false;
            }

            var item = quote.QuoteItems
                .FirstOrDefault(qi => qi.QuoteItemId == quoteItemId);
            if (item == null)
            {
                return false;
            }

            decimal itemDiscount = item.DiscountTypeId.HasValue ? (item.DiscountAmount ?? 0) * item.Quantity : 0;

            _context.QuoteItems.Remove(item);
            quote.TotalItemDiscounts -= itemDiscount;
            quote.TotalAmount = quote.QuoteItems.Sum(qi => qi.TotalPrice) - quote.TotalItemDiscounts - (quote.QuoteDiscountAmount ?? 0);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<QuoteDto> CopyQuoteAsync(int quoteId)
        {
            var originalQuote = await _context.Quotes
                .Include(q => q.QuoteItems)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (originalQuote == null)
            {
                throw new KeyNotFoundException($"Quote with ID {quoteId} not found.");
            }

            string newQuoteNumber = await GetNextQuoteNumberAsync();

            var newQuote = new Quote
            {
                QuoteNumber = newQuoteNumber,
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
                QuoteDiscountAmount = originalQuote.QuoteDiscountAmount,
                TotalItemDiscounts = originalQuote.TotalItemDiscounts,
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
                    NetDiscountedPrice = qi.NetDiscountedPrice,
                    ItemDescription = qi.ItemDescription,
                    VatTypeId = qi.VatTypeId,
                    TotalPrice = qi.TotalPrice,
                    DiscountTypeId = qi.DiscountTypeId,
                    DiscountAmount = qi.DiscountAmount
                }).ToList()
            };

            try
            {
                _context.Quotes.Add(newQuote);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new Exception($"Quote copied but failed to save. Inner error: {ex.InnerException?.Message}");
            }

            try
            {
                return await GetQuoteByIdAsync(newQuote.QuoteId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Quote saved but failed to retrieve. Inner error: {ex.Message}");
            }
        }

        // public async Task<OrderDto> ConvertQuoteToOrderAsync(int quoteId, ConvertQuoteToOrderDto convertDto, string createdBy)
        // {
        //     _logger.LogInformation("Starting conversion of quote ID: {QuoteId} to order. Requested by: {User}", quoteId, createdBy);

        //     // Step 1: Load quote and items
        //     var quote = await _context.Quotes
        //         .Include(q => q.QuoteItems)
        //         .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

        //     if (quote == null)
        //     {
        //         _logger.LogWarning("Quote not found for QuoteId: {QuoteId}", quoteId);
        //         throw new KeyNotFoundException($"Quote with ID {quoteId} not found");
        //     }

        //     _logger.LogInformation("Loaded quote #{QuoteId} with {ItemCount} item(s)", quote.QuoteId, quote.QuoteItems?.Count ?? 0);

        //     if (quote.QuoteItems == null || !quote.QuoteItems.Any())
        //     {
        //         _logger.LogWarning("Quote #{QuoteId} has no items to convert", quote.QuoteId);
        //         throw new InvalidOperationException("Quote must have at least one item to convert to an order");
        //     }

        //     // Step 2: Validate currency
        //     bool currencyExists = await _context.Currencies.AnyAsync(c => c.CurrencyId == convertDto.CurrencyId);
        //     _logger.LogInformation("Currency check for CurrencyId {CurrencyId}: {Exists}", convertDto.CurrencyId, currencyExists);

        //     if (!currencyExists)
        //     {
        //         _logger.LogWarning("Invalid CurrencyId: {CurrencyId}", convertDto.CurrencyId);
        //         throw new ArgumentException($"Invalid CurrencyId: {convertDto.CurrencyId}");
        //     }

        //     // Step 3: Validate site (optional)
        //     if (convertDto.SiteId.HasValue)
        //     {
        //         bool siteExists = await _context.Sites.AnyAsync(s => s.SiteId == convertDto.SiteId.Value);
        //         _logger.LogInformation("Site check for SiteId {SiteId}: {Exists}", convertDto.SiteId.Value, siteExists);

        //         if (!siteExists)
        //         {
        //             _logger.LogWarning("Invalid SiteId: {SiteId}", convertDto.SiteId);
        //             throw new ArgumentException($"Invalid SiteId: {convertDto.SiteId}");
        //         }
        //     }

        //     // Step 4: Build Order DTO
        //     var createOrderDto = new CreateOrderDto
        //     {
        //         PartnerId = quote.PartnerId,
        //         CurrencyId = convertDto.CurrencyId,
        //         // SiteId = convertDto.SiteId,
        //         QuoteId = quote.QuoteId,
        //         OrderDate = DateTime.UtcNow,
        //         TotalAmount = quote.TotalAmount,
        //         DiscountPercentage = quote.DiscountPercentage,
        //         DiscountAmount = quote.QuoteDiscountAmount,
        //         SalesPerson = quote.SalesPerson,
        //         Subject = quote.Subject,
        //         Description = quote.Description,
        //         DetailedDescription = quote.DetailedDescription,
        //         Status = "Draft",
        //         CreatedBy = createdBy,
        //         CreatedDate = DateTime.UtcNow,
        //         PaymentTerms = convertDto.PaymentTerms,
        //         ShippingMethod = convertDto.ShippingMethod,
        //         OrderType = convertDto.OrderType,
        //         OrderItems = quote.QuoteItems.Select(qi => new CreateOrderItemDto
        //         {
        //             ProductId = qi.ProductId,
        //             Quantity = qi.Quantity,
        //             UnitPrice = qi.NetDiscountedPrice,
        //             Description = qi.ItemDescription
        //         }).ToList()
        //     };

        //     _logger.LogInformation("Prepared CreateOrderDto with {ItemCount} item(s) for Quote #{QuoteId}", createOrderDto.OrderItems.Count, quote.QuoteId);

        //     // Step 5: Create the order
        //     var orderDto = await _orderService.CreateOrderAsync(CreateOrderAsync);
        //     _logger.LogInformation("Order created from quote #{QuoteId}. New order ID: {OrderId}", quote.QuoteId, orderDto?.OrderId);

        //     // Step 6: Update quote status
        //     quote.Status = "Converted";
        //     await _context.SaveChangesAsync();
        //     _logger.LogInformation("Updated quote #{QuoteId} status to Converted", quote.QuoteId);

        //     // Step 7: Retrieve full order DTO
        //     var fullOrderDto = await _orderService.GetOrderByIdAsync(orderDto.OrderId);
        //     if (fullOrderDto == null)
        //     {
        //         _logger.LogError("Order created but failed to load full DTO for order ID: {OrderId}", orderDto.OrderId);
        //         throw new InvalidOperationException("Failed to retrieve created order");
        //     }

        //     _logger.LogInformation("Successfully converted quote #{QuoteId} to order #{OrderId}", quote.QuoteId, orderDto.OrderId);
        //     return fullOrderDto;
        // }



        private decimal CalculateItemDiscount(QuoteItem item, decimal originalPrice)
        {
            if (!item.DiscountTypeId.HasValue || item.DiscountTypeId == (int)DiscountType.NoDiscount)
            {
                return 0;
            }

            return (item.DiscountAmount ?? 0) * item.Quantity;
        }
    }
}