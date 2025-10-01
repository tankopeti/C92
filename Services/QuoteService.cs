using AutoMapper;
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
    public class QuoteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<QuoteService> _logger;

        public QuoteService(ApplicationDbContext context, IMapper mapper, ILogger<QuoteService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Quote> CreateQuoteAsync(CreateQuoteDto createQuoteDto)
        {
            try
            {
                // Validate foreign keys
                if (createQuoteDto.PartnerId > 0 && !await _context.Partners.AnyAsync(p => p.PartnerId == createQuoteDto.PartnerId))
                {
                    throw new InvalidOperationException($"Partner with ID {createQuoteDto.PartnerId} not found.");
                }

                if (createQuoteDto.CurrencyId > 0 && !await _context.Currencies.AnyAsync(c => c.CurrencyId == createQuoteDto.CurrencyId))
                {
                    throw new InvalidOperationException($"Currency with ID {createQuoteDto.CurrencyId} not found.");
                }

                if (createQuoteDto.QuoteItems != null)
                {
                    foreach (var item in createQuoteDto.QuoteItems)
                    {
                        if (item.ProductId > 0 && !await _context.Products.AnyAsync(pr => pr.ProductId == item.ProductId))
                        {
                            throw new InvalidOperationException($"Product with ID {item.ProductId} not found.");
                        }

                        if (item.VatTypeId > 0 && !await _context.VatTypes.AnyAsync(v => v.VatTypeId == item.VatTypeId))
                        {
                            throw new InvalidOperationException($"VatType with ID {item.VatTypeId} not found.");
                        }

                        if (item.DiscountTypeId.HasValue && !Enum.IsDefined(typeof(DiscountType), item.DiscountTypeId.Value))
                        {
                            throw new InvalidOperationException($"Invalid DiscountTypeId {item.DiscountTypeId} for item.");
                        }
                    }
                }

                // Map DTO to Quote
                var quote = _mapper.Map<Quote>(createQuoteDto);
                quote.CreatedDate = DateTime.UtcNow;
                quote.ModifiedDate = DateTime.UtcNow;
                quote.Status ??= "Folyamatban";

                // Save Quote to get QuoteId
                _context.Quotes.Add(quote);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Quote created with ID {QuoteId}", quote.QuoteId);

                // Map and save QuoteItems
                if (createQuoteDto.QuoteItems != null && createQuoteDto.QuoteItems.Any())
                {
                    quote.QuoteItems = _mapper.Map<List<QuoteItem>>(createQuoteDto.QuoteItems);
                    foreach (var item in quote.QuoteItems)
                    {
                        item.QuoteId = quote.QuoteId;
                        _context.QuoteItems.Add(item);
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Quote {QuoteId} with {ItemCount} items saved successfully", quote.QuoteId, quote.QuoteItems?.Count ?? 0);
                return quote;
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger.LogError(dbEx, "Database error saving quote with QuoteNumber {QuoteNumber}: {InnerMessage}", createQuoteDto.QuoteNumber, innerMessage);
                throw new InvalidOperationException($"Failed to save quote: {innerMessage}", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quote with QuoteNumber {QuoteNumber}", createQuoteDto.QuoteNumber);
                throw;
            }
        }


        public async Task<Quote> UpdateQuoteAsync(UpdateQuoteDto updateQuoteDto)
        {
            try
            {
                var quote = await _context.Quotes
                    .Include(q => q.QuoteItems)
                    .FirstOrDefaultAsync(q => q.QuoteId == updateQuoteDto.QuoteId);

                if (quote == null)
                {
                    _logger.LogWarning("Quote with ID {QuoteId} not found", updateQuoteDto.QuoteId);
                    throw new KeyNotFoundException($"Quote with ID {updateQuoteDto.QuoteId} not found.");
                }

                _mapper.Map(updateQuoteDto, quote);
                quote.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated quote with ID {QuoteId}", quote.QuoteId);
                return quote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quote with ID {QuoteId}", updateQuoteDto.QuoteId);
                throw;
            }
        }

        public async Task<Quote> GetQuoteByIdAsync(int quoteId)
        {
            try
            {
                var quote = await _context.Quotes
                    .Include(q => q.QuoteItems)
                    .Include(q => q.Partner)
                    .Include(q => q.Currency)
                    .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

                if (quote == null)
                {
                    _logger.LogWarning("Quote with ID {QuoteId} not found", quoteId);
                    throw new KeyNotFoundException($"Quote with ID {quoteId} not found.");
                }

                return quote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quote with ID {QuoteId}", quoteId);
                throw;
            }
        }

        public async Task<List<Quote>> GetAllQuotesAsync()
        {
            try
            {
                var quotes = await _context.Quotes
                    .Include(q => q.QuoteItems)
                    .Include(q => q.Partner)
                    .Include(q => q.Currency)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} quotes", quotes.Count);
                return quotes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all quotes");
                throw;
            }
        }

        public async Task<bool> DeleteQuoteAsync(int quoteId)
        {
            try
            {
                var quote = await _context.Quotes.FindAsync(quoteId);
                if (quote == null)
                {
                    _logger.LogWarning("Quote with ID {QuoteId} not found", quoteId);
                    return false;
                }

                _context.Quotes.Remove(quote);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted quote with ID {QuoteId}", quoteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quote with ID {QuoteId}", quoteId);
                throw;
            }
        }
    }
}