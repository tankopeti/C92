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
                .Select(qi => new QuoteItemDto
                {
                    QuoteItemId = qi.QuoteItemId,
                    QuoteId = qi.QuoteId,
                    ProductId = qi.ProductId,
                    Quantity = qi.Quantity,
                    UnitPrice = qi.UnitPrice,
                    ItemDescription = qi.ItemDescription,
                    DiscountPercentage = qi.DiscountPercentage,
                    DiscountAmount = qi.DiscountAmount,
                    TotalPrice = qi.Quantity * qi.UnitPrice,
                    VatTypeId = qi.VatTypeId,
                    VatTypeName = qi.VatType != null ? qi.VatType.TypeName : null,
                    VatRate = qi.VatType != null ? qi.VatType.Rate : 0
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
                TotalAmount = quoteDto.TotalAmount ?? 0, // Ensure non-null for decimal
                SalesPerson = quoteDto.SalesPerson,
                ValidityDate = quoteDto.ValidityDate,
                Subject = quoteDto.Subject,
                Description = quoteDto.Description,
                DetailedDescription = quoteDto.DetailedDescription,
                DiscountPercentage = quoteDto.DiscountPercentage,
                DiscountAmount = quoteDto.DiscountAmount,
                CompanyName = quoteDto.CompanyName,
                CreatedBy = quoteDto.CreatedBy ?? "System",
                CreatedDate = quoteDto.CreatedDate ?? DateTime.UtcNow,
                ModifiedBy = quoteDto.ModifiedBy ?? "System",
                ModifiedDate = quoteDto.ModifiedDate ?? DateTime.UtcNow,
                ReferenceNumber = quoteDto.ReferenceNumber,
                QuoteItems = quoteDto.Items.Select(item => new QuoteItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPercentage = item.DiscountPercentage,
                    DiscountAmount = item.DiscountAmount
                }).ToList()
            };

            try
            {
                _context.Quotes.Add(quote);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Quote saved with ID: {QuoteId}", quote.QuoteId); // Log the QuoteId
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving quote");
                throw;
            }

            var result = new QuoteDto
            {
                QuoteId = quote.QuoteId, // Ensure the generated ID is included
                QuoteNumber = quote.QuoteNumber,
                PartnerId = quote.PartnerId,
                CurrencyId = quote.CurrencyId,
                QuoteDate = quote.QuoteDate,
                Status = quote.Status,
                TotalAmount = quote.TotalAmount,
                SalesPerson = quote.SalesPerson,
                ValidityDate = quote.ValidityDate,
                Subject = quote.Subject,
                Description = quote.Description,
                DetailedDescription = quote.DetailedDescription,
                DiscountPercentage = quote.DiscountPercentage,
                DiscountAmount = quote.DiscountAmount,
                CompanyName = quote.CompanyName,
                CreatedBy = quote.CreatedBy,
                CreatedDate = quote.CreatedDate,
                ModifiedBy = quote.ModifiedBy,
                ModifiedDate = quote.ModifiedDate,
                ReferenceNumber = quote.ReferenceNumber,
                Items = quote.QuoteItems.Select(qi => new QuoteItemDto
                {
                    QuoteItemId = qi.QuoteItemId,
                    QuoteId = qi.QuoteId,
                    ProductId = qi.ProductId,
                    Quantity = qi.Quantity,
                    UnitPrice = qi.UnitPrice,
                    DiscountPercentage = qi.DiscountPercentage,
                    DiscountAmount = qi.DiscountAmount,
                    TotalPrice = qi.TotalPrice
                }).ToList()
            };

            _logger.LogInformation("Returning QuoteDto with ID: {QuoteId}", result.QuoteId); // Log the returned QuoteId
            return result;
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
                    .ThenInclude(qi => qi.Product) // Include Product for QuoteItems
                .Include(q => q.Currency) // Ensure Currency is included
                .Where(q => q.QuoteId == quoteId)
                .Select(q => new QuoteDto
                {
                    QuoteId = q.QuoteId,
                    QuoteNumber = q.QuoteNumber,
                    PartnerId = q.PartnerId,
                    CurrencyId = q.CurrencyId, // Added CurrencyId
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
                    CompanyName = q.CompanyName, // Added Company Name
                    CreatedBy = q.CreatedBy, // Added CreatedBy
                    CreatedDate = q.CreatedDate, // Added CreatedDate
                    ModifiedBy = q.ModifiedBy, // Added ModifiedBy
                    ModifiedDate = q.ModifiedDate, // Added ModifiedDate
                    ReferenceNumber = q.ReferenceNumber, // Added ReferenceNumber
                    Currency = new CurrencyDto // Mapping CurrencyDto
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
                        DiscountPercentage = i.DiscountPercentage,
                        DiscountAmount = i.DiscountAmount,
                        TotalPrice = i.TotalPrice
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (quote == null)
                return new QuoteDto(); // Return an empty QuoteDto if not found

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

            if (_context == null)
            {
                _logger.LogError("Database context is null for UpdateQuoteAsync QuoteId: {QuoteId}", quoteId);
                throw new InvalidOperationException("Adatbázis kapcsolat nem érhető el");
            }

            var quote = await _context.Quotes.FirstOrDefaultAsync(q => q.QuoteId == quoteId);
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
            return new QuoteDto
            {
                QuoteId = quote.QuoteId,
                QuoteNumber = quote.QuoteNumber,
                CurrencyId = quote.CurrencyId,
                PartnerId = quote.PartnerId,
                QuoteDate = quote.QuoteDate,
                Status = quote.Status,
                TotalAmount = quote.TotalAmount,
                SalesPerson = quote.SalesPerson,
                ValidityDate = quote.ValidityDate,
                Subject = quote.Subject,
                Description = quote.Description,
                DetailedDescription = quote.DetailedDescription
            };
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

            if (_context == null)
            {
                _logger.LogError("Database context is null for CreateQuoteItemAsync QuoteId: {QuoteId}", quoteId);
                throw new InvalidOperationException("Adatbázis kapcsolat nem érhető el");
            }

            var quote = await _context.Quotes.FirstOrDefaultAsync(q => q.QuoteId == quoteId);
            if (quote == null)
            {
                _logger.LogWarning("Quote not found for QuoteId: {QuoteId}", quoteId);
                return null;
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == itemDto.ProductId);
            if (product == null)
            {
                _logger.LogWarning("Product not found for ProductId: {ProductId}", itemDto.ProductId);
                throw new ArgumentException($"Érvénytelen ProductId: {itemDto.ProductId}");
            }

            var quoteItem = new QuoteItem
            {
                QuoteId = quoteId,
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                ItemDescription = itemDto.ItemDescription ?? "", // Ensure empty string if null
                DiscountPercentage = itemDto.DiscountPercentage,
                DiscountAmount = itemDto.DiscountAmount
            };

            _context.QuoteItems.Add(quoteItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created quote item ID: {QuoteItemId} for QuoteId: {QuoteId}", quoteItem.QuoteItemId, quoteId);
            return new QuoteItemResponseDto
            {
                QuoteItemId = quoteItem.QuoteItemId,
                QuoteId = quoteItem.QuoteId,
                ProductId = quoteItem.ProductId,
                Quantity = quoteItem.Quantity,
                UnitPrice = quoteItem.UnitPrice,
                ItemDescription = quoteItem.ItemDescription,
                DiscountPercentage = quoteItem.DiscountPercentage,
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

            if (_context == null)
            {
                _logger.LogError("Database context is null for UpdateQuoteItemAsync QuoteId: {QuoteId}", quoteId);
                throw new InvalidOperationException("Adatbázis kapcsolat nem érhető el");
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

            quoteItem.ProductId = itemDto.ProductId;
            quoteItem.Quantity = itemDto.Quantity;
            quoteItem.UnitPrice = itemDto.UnitPrice;
            quoteItem.ItemDescription = itemDto.ItemDescription;
            quoteItem.DiscountPercentage = itemDto.DiscountPercentage;
            quoteItem.DiscountAmount = itemDto.DiscountAmount;

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
                DiscountPercentage = quoteItem.DiscountPercentage,
                DiscountAmount = quoteItem.DiscountAmount
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
                QuoteItems = originalQuote.QuoteItems.Select(qi => new QuoteItem
                {
                    ProductId = qi.ProductId,
                    Quantity = qi.Quantity,
                    UnitPrice = qi.UnitPrice,
                    ItemDescription = qi.ItemDescription,
                    DiscountPercentage = qi.DiscountPercentage,
                    DiscountAmount = qi.DiscountAmount
                }).ToList()
            };

            _context.Quotes.Add(newQuote);
            await _context.SaveChangesAsync();

            return new QuoteDto
            {
                QuoteId = newQuote.QuoteId,
                QuoteNumber = newQuote.QuoteNumber,
                PartnerId = newQuote.PartnerId,
                CurrencyId = newQuote.CurrencyId,
                QuoteDate = newQuote.QuoteDate,
                Status = newQuote.Status,
                TotalAmount = newQuote.TotalAmount
            };
        }

        public async Task<OrderDto> ConvertQuoteToOrderAsync(int quoteId, ConvertQuoteToOrderDto convertDto, string createdBy)
        {
            _logger.LogInformation("Converting quote ID: {QuoteId} to order", quoteId);

            var quote = await _context.Quotes
                .Include(q => q.QuoteItems)
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

            // Validate CurrencyId
            if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == convertDto.CurrencyId))
            {
                _logger.LogWarning("Invalid CurrencyId: {CurrencyId}", convertDto.CurrencyId);
                throw new ArgumentException($"Invalid CurrencyId: {convertDto.CurrencyId}");
            }

            // Validate SiteId if provided
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
                    DiscountPercentage = qi.DiscountPercentage,
                    DiscountAmount = qi.DiscountAmount
                }).ToList()
            };

            var orderDto = await _orderService.CreateOrderAsync(createOrderDto); // Use injected service

            // Update Quote status
            quote.Status = "Converted";
            await _context.SaveChangesAsync();

            // Fetch full OrderDto since CreateOrderAsync returns minimal DTO
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