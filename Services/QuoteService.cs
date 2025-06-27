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
                    NetDiscountedPrice = qi.NetDiscountedPrice,
                    ItemDescription = qi.ItemDescription,
                    TotalPrice = qi.TotalPrice,
                    VatTypeId = qi.VatTypeId,
                    DiscountType = qi.Discount != null ? qi.Discount.DiscountType : null,
                    DiscountPercentage = qi.Discount != null ? qi.Discount.DiscountPercentage : null,
                    DiscountAmount = qi.Discount != null ? qi.Discount.DiscountAmount : null,
                    BasePrice = qi.Discount != null ? qi.Discount.BasePrice : null,
                    PartnerPrice = qi.Discount != null ? qi.Discount.PartnerPrice : null,
                    VolumeThreshold = qi.Discount != null ? qi.Discount.VolumeThreshold : null,
                    VolumePrice = qi.Discount != null ? qi.Discount.VolumePrice : null,
                    ListPrice = qi.Discount != null ? qi.Discount.ListPrice : null
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

                QuoteItemDiscount discount = null;
                if (itemDto.DiscountType.HasValue && itemDto.DiscountType != DiscountType.NoDiscount)
                {
                    _logger.LogInformation("Processing discount for item {ProductId}: Type={DiscountType}", itemDto.ProductId, itemDto.DiscountType);
                    discount = new QuoteItemDiscount
                    {
                        DiscountType = itemDto.DiscountType.Value,
                        DiscountPercentage = itemDto.DiscountPercentage,
                        DiscountAmount = itemDto.DiscountAmount,
                        PartnerPrice = itemDto.PartnerPrice,
                        VolumeThreshold = itemDto.VolumeThreshold,
                        VolumePrice = itemDto.VolumePrice,
                        ListPrice = itemDto.ListPrice,
                        BasePrice = basePrice
                    };

                    switch (itemDto.DiscountType)
                    {
                        case DiscountType.CustomDiscountPercentage:
                            if (itemDto.DiscountPercentage < 0 || itemDto.DiscountPercentage > 100)
                            {
                                _logger.LogWarning("Invalid DiscountPercentage: {DiscountPercentage} for ProductId: {ProductId}", itemDto.DiscountPercentage, itemDto.ProductId);
                                throw new ArgumentException($"DiscountPercentage must be between 0 and 100 for ProductId: {itemDto.ProductId}");
                            }
                            totalItemDiscounts += (basePrice * itemDto.Quantity * itemDto.DiscountPercentage.Value / 100);
                            netDiscountedPrice = basePrice * (1 - itemDto.DiscountPercentage.Value / 100);
                            break;
                        case DiscountType.CustomDiscountAmount:
                            if (itemDto.DiscountAmount < 0)
                            {
                                _logger.LogWarning("Invalid DiscountAmount: {DiscountAmount} for ProductId: {ProductId}", itemDto.DiscountAmount, itemDto.ProductId);
                                throw new ArgumentException($"DiscountAmount must be non-negative for ProductId: {itemDto.ProductId}");
                            }
                            totalItemDiscounts += itemDto.DiscountAmount.Value;
                            netDiscountedPrice = (basePrice * itemDto.Quantity - itemDto.DiscountAmount.Value) / itemDto.Quantity;
                            break;
                        case DiscountType.PartnerPrice:
                            if (itemDto.PartnerPrice <= 0)
                            {
                                _logger.LogWarning("Invalid PartnerPrice: {PartnerPrice} for ProductId: {ProductId}", itemDto.PartnerPrice, itemDto.ProductId);
                                throw new ArgumentException($"PartnerPrice must be positive for ProductId: {itemDto.ProductId}");
                            }
                            totalItemDiscounts += (basePrice - itemDto.PartnerPrice.Value) * itemDto.Quantity;
                            netDiscountedPrice = itemDto.PartnerPrice.Value;
                            break;
                        case DiscountType.VolumeDiscount:
                            if (itemDto.VolumeThreshold <= 0 || itemDto.VolumePrice <= 0)
                            {
                                _logger.LogWarning("Invalid VolumeDiscount: Threshold={VolumeThreshold}, Price={VolumePrice} for ProductId: {ProductId}", 
                                    itemDto.VolumeThreshold, itemDto.VolumePrice, itemDto.ProductId);
                                throw new ArgumentException($"VolumeThreshold and VolumePrice must be positive for ProductId: {itemDto.ProductId}");
                            }
                            if (itemDto.Quantity >= itemDto.VolumeThreshold.Value)
                            {
                                totalItemDiscounts += (basePrice - itemDto.VolumePrice.Value) * itemDto.Quantity;
                                netDiscountedPrice = itemDto.VolumePrice.Value;
                            }
                            break;
                        default:
                            _logger.LogWarning("Unknown DiscountType: {DiscountType} for ProductId: {ProductId}", itemDto.DiscountType, itemDto.ProductId);
                            throw new ArgumentException($"Unknown DiscountType: {itemDto.DiscountType}");
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
                    Discount = discount
                };

                quote.QuoteItems.Add(quoteItem);
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
                    .ThenInclude(qi => qi.Discount)
                .Include(q => q.Currency)
                .Include(q => q.Partner)
                .Where(q => q.QuoteId == quoteId)
                .Select(q => new QuoteDto
                {
                    QuoteId = q.QuoteId,
                    QuoteNumber = q.QuoteNumber,
                    PartnerId = q.PartnerId,
                    CurrencyId = q.CurrencyId,
                    Currency = new CurrencyDto
                    {
                        CurrencyId = q.Currency.CurrencyId,
                        CurrencyName = q.Currency.CurrencyName
                    },
                    Partner = new PartnerDto
                    {
                        PartnerId = q.Partner.PartnerId,
                        Name = q.Partner.Name
                    },
                    QuoteDate = q.QuoteDate,
                    Status = q.Status,
                    TotalAmount = q.TotalAmount,
                    SalesPerson = q.SalesPerson,
                    ValidityDate = q.ValidityDate,
                    Subject = q.Subject,
                    Description = q.Description,
                    DetailedDescription = q.DetailedDescription,
                    DiscountPercentage = q.DiscountPercentage,
                    QuoteDiscountAmount = q.QuoteDiscountAmount,
                    TotalItemDiscounts = q.TotalItemDiscounts,
                    CompanyName = q.CompanyName,
                    CreatedBy = q.CreatedBy,
                    CreatedDate = q.CreatedDate,
                    ModifiedBy = q.ModifiedBy,
                    ModifiedDate = q.ModifiedDate,
                    ReferenceNumber = q.ReferenceNumber,
                    Items = q.QuoteItems.Select(i => new QuoteItemDto
                    {
                        QuoteItemId = i.QuoteItemId,
                        QuoteId = i.QuoteId,
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        NetDiscountedPrice = i.NetDiscountedPrice,
                        ItemDescription = i.ItemDescription,
                        TotalPrice = i.TotalPrice,
                        VatTypeId = i.VatTypeId,
                        DiscountType = i.Discount != null ? i.Discount.DiscountType : null,
                        DiscountPercentage = i.Discount != null ? i.Discount.DiscountPercentage : null,
                        DiscountAmount = i.Discount != null ? i.Discount.DiscountAmount : null,
                        BasePrice = i.Discount != null ? i.Discount.BasePrice : null,
                        PartnerPrice = i.Discount != null ? i.Discount.PartnerPrice : null,
                        VolumeThreshold = i.Discount != null ? i.Discount.VolumeThreshold : null,
                        VolumePrice = i.Discount != null ? i.Discount.VolumePrice : null,
                        ListPrice = i.Discount != null ? i.Discount.ListPrice : null
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (quote == null)
            {
                _logger.LogWarning("Quote not found for QuoteId: {QuoteId}", quoteId);
                return new QuoteDto();
            }

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
            decimal netDiscountedPrice = basePrice;

            QuoteItemDiscount discount = null;
            decimal itemDiscount = 0;

            if (itemDto.DiscountType.HasValue && itemDto.DiscountType != DiscountType.NoDiscount)
            {
                discount = new QuoteItemDiscount
                {
                    DiscountType = itemDto.DiscountType.Value,
                    DiscountPercentage = itemDto.DiscountPercentage,
                    DiscountAmount = itemDto.DiscountAmount,
                    PartnerPrice = itemDto.PartnerPrice,
                    VolumeThreshold = itemDto.VolumeThreshold,
                    VolumePrice = itemDto.VolumePrice,
                    ListPrice = itemDto.ListPrice ,
                    BasePrice = basePrice
                };

                switch (itemDto.DiscountType)
                {
                    case DiscountType.CustomDiscountPercentage:
                        discount.DiscountPercentage = discount.DiscountPercentage ?? 0;
                        itemDiscount = basePrice * itemDto.Quantity * discount.DiscountPercentage.Value / 100;
                        netDiscountedPrice = basePrice * (1 - discount.DiscountPercentage.Value / 100);
                        discount.DiscountAmount = null;
                        discount.PartnerPrice = null;
                        discount.VolumeThreshold = null;
                        discount.VolumePrice = null;
                        break;
                    case DiscountType.CustomDiscountAmount:
                        discount.DiscountAmount = discount.DiscountAmount ?? 0;
                        itemDiscount = discount.DiscountAmount.Value;
                        netDiscountedPrice = (basePrice * itemDto.Quantity - discount.DiscountAmount.Value) / itemDto.Quantity;
                        discount.DiscountPercentage = null;
                        discount.PartnerPrice = null;
                        discount.VolumeThreshold = null;
                        discount.VolumePrice = null;
                        break;
                    case DiscountType.PartnerPrice:
                        basePrice = partnerPrice?.PartnerUnitPrice ?? basePrice;
                        itemDiscount = (productPrice.SalesPrice - basePrice) * itemDto.Quantity;
                        netDiscountedPrice = basePrice;
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
                        itemDiscount = (productPrice.SalesPrice - basePrice) * itemDto.Quantity;
                        netDiscountedPrice = basePrice;
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
                NetDiscountedPrice = netDiscountedPrice,
                ItemDescription = itemDto.ItemDescription ?? "",
                VatTypeId = itemDto.VatTypeId,
                TotalPrice = itemDto.Quantity * netDiscountedPrice,
                Discount = discount
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
                DiscountType = discount?.DiscountType,
                DiscountPercentage = discount?.DiscountPercentage,
                DiscountAmount = discount?.DiscountAmount,
                BasePrice = discount?.BasePrice,
                PartnerPrice = discount?.PartnerPrice,
                VolumeThreshold = discount?.VolumeThreshold,
                VolumePrice = discount?.VolumePrice,
                ListPrice = discount?.ListPrice
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
                .Where(pp => pp.ProductId == itemDto.ProductId && pp.PartnerId == quote.PartnerId)
                .FirstOrDefaultAsync();

            decimal basePrice = productPrice?.SalesPrice ?? throw new InvalidOperationException($"No active price found for ProductId: {itemDto.ProductId}");
            decimal netDiscountedPrice = basePrice;
            decimal oldItemDiscount = quoteItem.Discount != null ? CalculateItemDiscount(quoteItem, productPrice.SalesPrice) : 0;
            decimal newItemDiscount = 0;

            if (quoteItem.Discount != null)
            {
                _context.QuoteItemDiscounts.Remove(quoteItem.Discount);
            }

            QuoteItemDiscount discount = null;
            if (itemDto.DiscountType.HasValue && itemDto.DiscountType != DiscountType.NoDiscount)
            {
                discount = new QuoteItemDiscount
                {
                    QuoteItemId = quoteItem.QuoteItemId,
                    DiscountType = itemDto.DiscountType.Value,
                    DiscountPercentage = itemDto.DiscountPercentage,
                    DiscountAmount = itemDto.DiscountAmount,
                    PartnerPrice = itemDto.PartnerPrice,
                    VolumeThreshold = itemDto.VolumeThreshold,
                    VolumePrice = itemDto.VolumePrice,
                    ListPrice = itemDto.ListPrice,
                    BasePrice = basePrice
                };

                switch (itemDto.DiscountType)
                {
                    case DiscountType.CustomDiscountPercentage:
                        discount.DiscountPercentage = discount.DiscountPercentage ?? 0;
                        newItemDiscount = basePrice * itemDto.Quantity * discount.DiscountPercentage.Value / 100;
                        netDiscountedPrice = basePrice * (1 - discount.DiscountPercentage.Value / 100);
                        discount.DiscountAmount = null;
                        discount.PartnerPrice = null;
                        discount.VolumeThreshold = null;
                        discount.VolumePrice = null;
                        break;
                    case DiscountType.CustomDiscountAmount:
                        discount.DiscountAmount = discount.DiscountAmount ?? 0;
                        newItemDiscount = discount.DiscountAmount.Value;
                        netDiscountedPrice = (basePrice * itemDto.Quantity - discount.DiscountAmount.Value) / itemDto.Quantity;
                        discount.DiscountPercentage = null;
                        discount.PartnerPrice = null;
                        discount.VolumeThreshold = null;
                        discount.VolumePrice = null;
                        break;
                    case DiscountType.PartnerPrice:
                        basePrice = partnerPrice?.PartnerUnitPrice ?? basePrice;
                        newItemDiscount = (productPrice.SalesPrice - basePrice) * itemDto.Quantity;
                        netDiscountedPrice = basePrice;
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
                        newItemDiscount = (productPrice.SalesPrice - basePrice) * itemDto.Quantity;
                        netDiscountedPrice = basePrice;
                        discount.DiscountPercentage = null;
                        discount.DiscountAmount = null;
                        discount.PartnerPrice = null;
                        break;
                }
                discount.BasePrice = basePrice;
            }

            quoteItem.ProductId = itemDto.ProductId;
            quoteItem.Quantity = itemDto.Quantity;
            quoteItem.NetDiscountedPrice = netDiscountedPrice;
            quoteItem.ItemDescription = itemDto.ItemDescription;
            quoteItem.VatTypeId = itemDto.VatTypeId;
            quoteItem.TotalPrice = itemDto.Quantity * netDiscountedPrice;
            quoteItem.Discount = discount;

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
                DiscountType = discount?.DiscountType,
                DiscountPercentage = discount?.DiscountPercentage,
                DiscountAmount = discount?.DiscountAmount,
                BasePrice = discount?.BasePrice,
                PartnerPrice = discount?.PartnerPrice,
                VolumeThreshold = discount?.VolumeThreshold,
                VolumePrice = discount?.VolumePrice,
                ListPrice = discount?.ListPrice
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

            decimal itemDiscount = item.Discount != null ? CalculateItemDiscount(item, item.NetDiscountedPrice) : 0;

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
                    Discount = qi.Discount != null ? new QuoteItemDiscount
                    {
                        DiscountType = qi.Discount.DiscountType,
                        DiscountPercentage = qi.Discount.DiscountPercentage,
                        DiscountAmount = qi.Discount.DiscountAmount,
                        BasePrice = qi.Discount.BasePrice,
                        PartnerPrice = qi.Discount.PartnerPrice,
                        VolumeThreshold = qi.Discount.VolumeThreshold,
                        VolumePrice = qi.Discount.VolumePrice,
                        ListPrice = qi.Discount.ListPrice
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
                DiscountAmount = quote.QuoteDiscountAmount,
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
                    UnitPrice = qi.NetDiscountedPrice,
                    Description = qi.ItemDescription
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

        private decimal CalculateItemDiscount(QuoteItem item, decimal originalPrice)
        {
            if (item.Discount == null) return 0;

            switch (item.Discount.DiscountType)
            {
                case DiscountType.CustomDiscountPercentage:
                    return item.Discount.DiscountPercentage.HasValue ? originalPrice * item.Quantity * item.Discount.DiscountPercentage.Value / 100 : 0;
                case DiscountType.CustomDiscountAmount:
                    return item.Discount.DiscountAmount ?? 0;
                case DiscountType.PartnerPrice:
                    return (originalPrice - (item.Discount.PartnerPrice ?? originalPrice)) * item.Quantity;
                case DiscountType.VolumeDiscount:
                    return (originalPrice - (item.Discount.VolumePrice ?? originalPrice)) * item.Quantity;
                default:
                    return 0;
            }
        }
    }
}