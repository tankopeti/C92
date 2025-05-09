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

        public async Task<QuoteDto> GetQuoteByIdAsync(int quoteId)
        {
            var quote = await _context.Quotes
                .Where(q => q.QuoteId == quoteId)
                .Select(q => new QuoteDto
                {
                    QuoteId = q.QuoteId,
                    QuoteNumber = q.QuoteNumber,
                    PartnerId = q.PartnerId,
                    QuoteDate = q.QuoteDate,
                    Status = q.Status,
                    TotalAmount = q.TotalAmount
                })
                .FirstOrDefaultAsync();

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

        public async Task<QuoteItemDto> CreateQuoteItemAsync(int quoteId, CreateQuoteItemDto itemDto)
        {
            var quote = await _context.Quotes
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (quote == null)
            {
                return null;
            }

            var item = new QuoteItem
            {
                QuoteId = quoteId,
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                ItemDescription = itemDto.ItemDescription,
                DiscountPercentage = itemDto.DiscountPercentage,
                DiscountAmount = itemDto.DiscountAmount
            };

            _context.QuoteItems.Add(item);
            await _context.SaveChangesAsync();

            return new QuoteItemDto
            {
                QuoteItemId = item.QuoteItemId,
                QuoteId = item.QuoteId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                ItemDescription = item.ItemDescription,
                DiscountPercentage = item.DiscountPercentage,
                DiscountAmount = item.DiscountAmount,
                TotalPrice = item.Quantity * item.UnitPrice
            };
        }

        public async Task<QuoteItemDto> UpdateQuoteItemAsync(int quoteId, int quoteItemId, UpdateQuoteItemDto itemDto)
        {
            _logger.LogInformation("Updating quote item ID: {QuoteItemId} for QuoteId: {QuoteId}", quoteItemId, quoteId);

            var item = await _context.QuoteItems
                .FirstOrDefaultAsync(qi => qi.QuoteId == quoteId && qi.QuoteItemId == quoteItemId);

            if (item == null)
            {
                _logger.LogWarning("Quote item not found: QuoteId {QuoteId}, QuoteItemId {QuoteItemId}", quoteId, quoteItemId);
                return null;
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == itemDto.ProductId);
            if (product == null)
            {
                _logger.LogWarning("Product not found: {ProductId}", itemDto.ProductId);
                return null;
            }

            item.ProductId = itemDto.ProductId;
            item.Quantity = itemDto.Quantity;
            item.UnitPrice = itemDto.UnitPrice;
            item.ItemDescription = itemDto.ItemDescription;
            item.DiscountPercentage = itemDto.DiscountPercentage;
            item.DiscountAmount = itemDto.DiscountAmount;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated quote item ID: {QuoteItemId} for QuoteId: {QuoteId}", quoteItemId, quoteId);

            return new QuoteItemDto
            {
                QuoteItemId = item.QuoteItemId,
                QuoteId = item.QuoteId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                ItemDescription = item.ItemDescription,
                DiscountPercentage = item.DiscountPercentage,
                DiscountAmount = item.DiscountAmount,
                TotalPrice = item.Quantity * item.UnitPrice
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