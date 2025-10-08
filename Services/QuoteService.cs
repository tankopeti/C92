using AutoMapper;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Controllers;
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

        public async Task<Quote> CreateQuoteAsync(CreateQuoteDto createQuoteDto, string createdBy)
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

                // Generate unique QuoteNumber
                string quoteNumber;
                do
                {
                    quoteNumber = GenerateQuoteNumber(); // Implement your logic, e.g., "Q-YYYYMMDD-XXXX"
                } while (await _context.Quotes.AnyAsync(q => q.QuoteNumber == quoteNumber));

                // Map DTO to Quote
                var quote = _mapper.Map<Quote>(createQuoteDto);
                quote.QuoteNumber = quoteNumber;
                quote.CreatedDate = DateTime.UtcNow;
                quote.ModifiedDate = DateTime.UtcNow;
                quote.CreatedBy = createdBy; // Use the passed username
                quote.Status ??= "Folyamatban";

                // Save Quote to get QuoteId
                _context.Quotes.Add(quote);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Quote created with ID {QuoteId} by {CreatedBy}", quote.QuoteId, createdBy);

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

        private string GenerateQuoteNumber()
        {
            // Example: Q-20251008-1234
            return $"Q-{DateTime.Today:yyyyMMdd}-{new Random().Next(1000, 9999)}";
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

                // Validate foreign keys
                if (!await _context.Partners.AnyAsync(p => p.PartnerId == updateQuoteDto.PartnerId))
                    throw new ArgumentException($"Invalid PartnerId: {updateQuoteDto.PartnerId}");
                if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == updateQuoteDto.CurrencyId))
                    throw new ArgumentException($"Invalid CurrencyId: {updateQuoteDto.CurrencyId}");
                if (updateQuoteDto.QuoteItems != null)
                {
                    foreach (var item in updateQuoteDto.QuoteItems)
                    {
                        if (!await _context.Products.AnyAsync(p => p.ProductId == item.ProductId))
                            throw new ArgumentException($"Invalid ProductId: {item.ProductId}");
                        if (!await _context.VatTypes.AnyAsync(v => v.VatTypeId == item.VatTypeId))
                            throw new ArgumentException($"Invalid VatTypeId: {item.VatTypeId}");
                    }
                }

                // Update quote properties
                quote.QuoteNumber = string.IsNullOrEmpty(updateQuoteDto.QuoteNumber)
                    ? $"Q-{DateTime.Today:yyyyMMdd}-{new Random().Next(1000, 9999)}"
                    : updateQuoteDto.QuoteNumber;
                quote.QuoteDate = updateQuoteDto.QuoteDate ?? quote.QuoteDate;
                quote.PartnerId = updateQuoteDto.PartnerId ?? quote.PartnerId;
                quote.CurrencyId = updateQuoteDto.CurrencyId ?? quote.CurrencyId;
                quote.SalesPerson = updateQuoteDto.SalesPerson ?? quote.SalesPerson;
                quote.ValidityDate = updateQuoteDto.ValidityDate ?? quote.ValidityDate;
                quote.Subject = updateQuoteDto.Subject ?? quote.Subject;
                quote.Description = updateQuoteDto.Description ?? quote.Description;
                quote.DetailedDescription = updateQuoteDto.DetailedDescription ?? quote.DetailedDescription;
                quote.Status = updateQuoteDto.Status ?? quote.Status;
                quote.DiscountPercentage = updateQuoteDto.DiscountPercentage ?? quote.DiscountPercentage;
                quote.TotalAmount = updateQuoteDto.TotalAmount ?? quote.TotalAmount;
                quote.ModifiedDate = DateTime.UtcNow;
                quote.ModifiedBy = updateQuoteDto.ModifiedBy ?? "System";

                // Handle QuoteItems
                if (updateQuoteDto.QuoteItems != null)
                {
                    // Remove items not in the DTO
                    var existingItemIds = updateQuoteDto.QuoteItems
                        .Where(i => i.QuoteItemId > 0)
                        .Select(i => i.QuoteItemId)
                        .ToList();
                    var itemsToRemove = quote.QuoteItems
                        .Where(i => !existingItemIds.Contains(i.QuoteItemId))
                        .ToList();
                    foreach (var item in itemsToRemove)
                    {
                        _context.QuoteItems.Remove(item);
                    }

                    // Add or update items
                    foreach (var itemDto in updateQuoteDto.QuoteItems)
                    {
                        var item = quote.QuoteItems.FirstOrDefault(i => i.QuoteItemId == itemDto.QuoteItemId);
                        if (item == null)
                        {
                            item = new QuoteItem { QuoteId = quote.QuoteId };
                            _context.QuoteItems.Add(item);
                            quote.QuoteItems.Add(item);
                        }
                        item.ProductId = itemDto.ProductId;
                        item.Quantity = itemDto.Quantity;
                        item.ListPrice = itemDto.ListPrice;
                        item.NetDiscountedPrice = itemDto.NetDiscountedPrice;
                        item.TotalPrice = itemDto.TotalPrice;
                        item.VatTypeId = itemDto.VatTypeId;
                        item.DiscountTypeId = itemDto.DiscountTypeId ?? 1;
                        item.DiscountAmount = itemDto.DiscountAmount ?? 0;
                        item.PartnerPrice = itemDto.PartnerPrice;
                        item.VolumePrice = itemDto.VolumePrice;
                    }
                }

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

        public async Task<bool> DeleteQuoteAsync(int id)
        {
            try
            {
                var quote = await _context.Quotes
                    .Include(q => q.QuoteItems)
                    .FirstOrDefaultAsync(q => q.QuoteId == id);

                if (quote == null)
                {
                    _logger.LogWarning("Quote with ID {QuoteId} not found", id);
                    return false;
                }

                // Remove associated QuoteItems
                _context.QuoteItems.RemoveRange(quote.QuoteItems);

                // Remove the quote
                _context.Quotes.Remove(quote);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted quote with ID {QuoteId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting quote with ID {QuoteId}: {InnerException}", id, ex.InnerException?.Message);
                throw new Exception($"Failed to delete quote: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quote with ID {QuoteId}", id);
                throw;
            }
        }


    }
}