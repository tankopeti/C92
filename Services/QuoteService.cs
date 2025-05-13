using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
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

        public QuoteService(ApplicationDbContext context, ILogger<QuoteService> logger)
        {
            _context = context;
            _logger = logger;
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
                    TotalPrice = qi.Quantity * qi.UnitPrice
                })
                .ToListAsync();
        }

        public async Task<QuoteDto> CreateQuoteAsync(CreateQuoteDto quoteDto)
        {
            var quote = new Quote
            {
                QuoteNumber = await GetNextQuoteNumberAsync(),
                PartnerId = quoteDto.PartnerId,
                QuoteDate = quoteDto.QuoteDate ?? DateTime.UtcNow,
                Status = "Draft",
                TotalAmount = quoteDto.TotalAmount
            };

            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();

            return new QuoteDto
            {
                QuoteId = quote.QuoteId,
                QuoteNumber = quote.QuoteNumber,
                PartnerId = quote.PartnerId,
                QuoteDate = quote.QuoteDate,
                Status = quote.Status,
                TotalAmount = quote.TotalAmount
            };
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
                .Where(q => q.QuoteId == quoteId)
                .Select(q => new QuoteDto
                {
                    QuoteId = q.QuoteId,
                    QuoteNumber = q.QuoteNumber,
                    PartnerId = q.PartnerId,
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
                    Items = q.QuoteItems.Select(i => new QuoteItemDto
                    {
                        QuoteItemId = i.QuoteItemId,
                        QuoteId = i.QuoteId,
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name ?? "Ismeretlen", // Set ProductName directly
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
            var quote = await _context.Quotes
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (quote == null)
            {
                return null;
            }

            quote.PartnerId = quoteDto.PartnerId;
            quote.QuoteDate = quoteDto.QuoteDate ?? quote.QuoteDate;
            quote.Status = quoteDto.Status ?? quote.Status;
            quote.TotalAmount = quoteDto.TotalAmount ?? quote.TotalAmount;

            await _context.SaveChangesAsync();

            return new QuoteDto
            {
                QuoteId = quote.QuoteId,
                QuoteNumber = quote.QuoteNumber,
                PartnerId = quote.PartnerId,
                QuoteDate = quote.QuoteDate,
                Status = quote.Status,
                TotalAmount = quote.TotalAmount
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
    _logger.LogInformation("Creating quote item for QuoteId: {QuoteId}", quoteId);

    var quote = await _context.Quotes
        .Include(q => q.QuoteItems)
        .FirstOrDefaultAsync(q => q.QuoteId == quoteId);
    if (quote == null)
    {
        _logger.LogWarning("Quote not found: {QuoteId}", quoteId);
        throw new KeyNotFoundException("Quote not found");
    }

    var product = await _context.Products
        .FirstOrDefaultAsync(p => p.ProductId == itemDto.ProductId);
    if (product == null)
    {
        _logger.LogWarning("Product not found: {ProductId}", itemDto.ProductId);
        throw new KeyNotFoundException("Product not found");
    }

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        var quoteItem = new QuoteItem
        {
            QuoteId = quoteId,
            ProductId = itemDto.ProductId,
            Quantity = itemDto.Quantity,
            UnitPrice = itemDto.UnitPrice,
            ItemDescription = itemDto.ItemDescription,
            DiscountPercentage = itemDto.DiscountPercentage,
            DiscountAmount = itemDto.DiscountAmount
        };

        _context.QuoteItems.Add(quoteItem);
        _logger.LogDebug("Before SaveChangesAsync for new quote item");
        await _context.SaveChangesAsync();
        _logger.LogDebug("After SaveChangesAsync for new quote item");

        quote.TotalAmount = quote.QuoteItems.Sum(qi =>
        {
            var baseAmount = qi.Quantity * qi.UnitPrice;
            var percentageDiscount = qi.DiscountPercentage.HasValue ? (baseAmount * qi.DiscountPercentage.Value / 100) : 0M;
            var totalDiscount = percentageDiscount + (qi.DiscountAmount ?? 0M);
            return baseAmount - totalDiscount;
        });

        if (quote.TotalAmount < 0)
        {
            _logger.LogWarning("Calculated negative TotalAmount for QuoteId: {QuoteId}", quoteId);
            throw new InvalidOperationException("Total amount cannot be negative");
        }

        _logger.LogDebug("Before SaveChangesAsync for quote {QuoteId} TotalAmount", quoteId);
        await _context.SaveChangesAsync();
        _logger.LogDebug("After SaveChangesAsync for quote {QuoteId} TotalAmount", quoteId);

        await transaction.CommitAsync();

        _logger.LogInformation("Created quote item ID: {QuoteItemId} for QuoteId: {QuoteId}", quoteItem.QuoteItemId, quoteId);

        return new QuoteItemResponseDto
        {
            QuoteItemId = quoteItem.QuoteItemId,
            QuoteId = quoteItem.QuoteId,
            ProductId = quoteItem.ProductId,
            Product = new ProductDto { Name = product.Name },
            Quantity = quoteItem.Quantity,
            UnitPrice = quoteItem.UnitPrice,
            ItemDescription = quoteItem.ItemDescription, // Keep null if null
            DiscountPercentage = quoteItem.DiscountPercentage,
            DiscountAmount = quoteItem.DiscountAmount,
            QuoteTotalAmount = quote.TotalAmount ?? 0M // Consider renaming to QuoteTotal
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create quote item for quote {QuoteId}", quoteId);
        await transaction.RollbackAsync();
        throw;
    }
}

public async Task<QuoteItemResponseDto> UpdateQuoteItemAsync(int quoteId, int quoteItemId, UpdateQuoteItemDto itemDto)
{
    _logger.LogInformation("Updating quote item ID: {QuoteItemId} for QuoteId: {QuoteId}", quoteItemId, quoteId);

    var quoteItem = await _context.QuoteItems
        .FirstOrDefaultAsync(qi => qi.QuoteId == quoteId && qi.QuoteItemId == quoteItemId);
    if (quoteItem == null)
    {
        _logger.LogWarning("Quote item not found: QuoteId {QuoteId}, QuoteItemId {QuoteItemId}", quoteId, quoteItemId);
        throw new KeyNotFoundException("Quote item not found");
    }

    var product = await _context.Products
        .FirstOrDefaultAsync(p => p.ProductId == itemDto.ProductId);
    if (product == null)
    {
        _logger.LogWarning("Product not found: {ProductId}", itemDto.ProductId);
        throw new KeyNotFoundException("Product not found");
    }

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        quoteItem.ProductId = itemDto.ProductId;
        quoteItem.Quantity = itemDto.Quantity;
        quoteItem.UnitPrice = itemDto.UnitPrice;
        quoteItem.ItemDescription = itemDto.ItemDescription;
        quoteItem.DiscountPercentage = itemDto.DiscountPercentage;
        quoteItem.DiscountAmount = itemDto.DiscountAmount;

        var quote = await _context.Quotes
            .Include(q => q.QuoteItems)
            .FirstOrDefaultAsync(q => q.QuoteId == quoteId);
        if (quote == null)
        {
            _logger.LogWarning("Quote not found: {QuoteId}", quoteId);
            throw new KeyNotFoundException("Quote not found");
        }

        quote.TotalAmount = quote.QuoteItems.Sum(qi =>
        {
            var baseAmount = qi.Quantity * qi.UnitPrice;
            var percentageDiscount = qi.DiscountPercentage.HasValue ? (baseAmount * qi.DiscountPercentage.Value / 100) : 0M;
            var totalDiscount = percentageDiscount + (qi.DiscountAmount ?? 0M);
            return baseAmount - totalDiscount;
        });

        if (quote.TotalAmount < 0)
        {
            _logger.LogWarning("Calculated negative TotalAmount for QuoteId: {QuoteId}", quoteId);
            throw new InvalidOperationException("Total amount cannot be negative");
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Updated quote item ID: {QuoteItemId} for QuoteId: {QuoteId}", quoteItemId, quoteId);

        return new QuoteItemResponseDto
        {
            QuoteItemId = quoteItem.QuoteItemId,
            QuoteId = quoteItem.QuoteId,
            ProductId = quoteItem.ProductId,
            Product = new ProductDto { Name = product.Name },
            Quantity = quoteItem.Quantity,
            UnitPrice = quoteItem.UnitPrice,
            ItemDescription = quoteItem.ItemDescription, // Already correct
            DiscountPercentage = quoteItem.DiscountPercentage,
            DiscountAmount = quoteItem.DiscountAmount,
            QuoteTotalAmount = quote.TotalAmount ?? 0M // Consider renaming to QuoteTotal
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to update quote item {QuoteItemId} for quote {QuoteId}", quoteItemId, quoteId);
        await transaction.RollbackAsync();
        throw;
    }
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
                QuoteDate = newQuote.QuoteDate,
                Status = newQuote.Status,
                TotalAmount = newQuote.TotalAmount
            };
        }
    }
}