using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.CRM.Quotes
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<Quote> Quotes { get; set; } = new List<Quote>();
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public int PageSize { get; set; } = 10;
        public string NextQuoteNumber { get; set; }

public async Task OnGetAsync(string searchTerm, int pageNumber = 1, int pageSize = 10)
{
    SearchTerm = searchTerm;
    PageSize = pageSize;
    CurrentPage = pageNumber;

    _logger.LogInformation("Executing OnGetAsync with searchTerm: '{SearchTerm}', pageNumber: {PageNumber}, pageSize: {PageSize}", 
        searchTerm ?? "null", pageNumber, pageSize);

    var query = _context.Quotes
        .AsNoTracking()
        .Select(q => new Quote
        {
            QuoteId = q.QuoteId,
            QuoteNumber = q.QuoteNumber,
            Status = q.Status,
            QuoteDate = q.QuoteDate,
            ValidityDate = q.ValidityDate,
            TotalAmount = q.TotalAmount,
            SalesPerson = q.SalesPerson,
            Subject = q.Subject,
            Description = q.Description,
            DetailedDescription = q.DetailedDescription,
            DiscountPercentage = q.DiscountPercentage,
            DiscountAmount = q.DiscountAmount,
            PartnerId = q.PartnerId,
            CurrencyId = q.CurrencyId,
            Partner = _context.Partners.SingleOrDefault(p => p.PartnerId == q.PartnerId),
            Currency = _context.Currencies.SingleOrDefault(c => c.CurrencyId == q.CurrencyId),
            QuoteItems = q.QuoteItems.Select(qi => new QuoteItem
            {
                QuoteItemId = qi.QuoteItemId,
                QuoteId = qi.QuoteId,
                ProductId = qi.ProductId,
                Product = _context.Products.SingleOrDefault(p => p.ProductId == qi.ProductId),
                ItemDescription = qi.ItemDescription,
                Quantity = qi.Quantity,
                UnitPrice = qi.UnitPrice,
                DiscountPercentage = qi.DiscountPercentage,
                DiscountAmount = qi.DiscountAmount
            }).ToList()
        })
        .AsQueryable();

    if (!string.IsNullOrEmpty(searchTerm))
    {
        _logger.LogInformation("Applying searchTerm filter: '{SearchTerm}'", searchTerm);
        query = query.Where(q => (q.QuoteNumber != null && q.QuoteNumber.Contains(searchTerm)) || 
                                (q.Partner != null && q.Partner.Name != null && q.Partner.Name.Contains(searchTerm)));
    }

    query = query.OrderByDescending(q => q.QuoteNumber ?? "");

    try
    {
        TotalRecords = await query.CountAsync();
        _logger.LogInformation("TotalRecords: {TotalRecords}", TotalRecords);

        if (TotalRecords > 0)
        {
            var quoteDetails = await query
                .Select(q => new { 
                    q.QuoteId, 
                    q.QuoteNumber, 
                    q.Status, 
                    PartnerName = q.Partner != null ? q.Partner.Name : null, 
                    CurrencyName = q.Currency != null ? q.Currency.CurrencyName : null,
                    QuoteItemsCount = q.QuoteItems.Count // Log item count
                })
                .Take(10)
                .ToListAsync();
            _logger.LogInformation("Quote details retrieved: {@QuoteDetails}", quoteDetails);
        }
        else
        {
            _logger.LogWarning("No quotes found in database");
        }

        TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);
        Quotes = await query
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} quotes: {@Quotes}", Quotes.Count, 
            Quotes.Select(q => new { 
                q.QuoteId, 
                q.QuoteNumber, 
                q.Status, 
                PartnerName = q.Partner?.Name, 
                CurrencyName = q.Currency?.CurrencyName,
                QuoteItemsCount = q.QuoteItems != null ? q.QuoteItems.Count : 0 
            }));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching quotes: {Message}", ex.Message);
        Quotes = new List<Quote>();
    }
}

public async Task<IActionResult> OnPostCreateQuoteAsync([FromBody] QuoteItemCreateDto dto)
{
    _logger.LogInformation("OnPostCreateQuoteAsync called with DTO: {@Dto}", dto);

    if (!ModelState.IsValid)
    {
        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        _logger.LogError("ModelState invalid: {Errors}", string.Join(", ", errors));
        return BadRequest(new { errors = errors.ToList() });
    }

    try
    {
        // Validate required fields
        if (dto.PartnerId <= 0)
        {
            _logger.LogWarning("Invalid PartnerId: {PartnerId}", dto.PartnerId);
            return BadRequest(new { error = "Partner kiválasztása kötelező." });
        }
        if (dto.CurrencyId <= 0)
        {
            _logger.LogWarning("Invalid CurrencyId: {CurrencyId}", dto.CurrencyId);
            return BadRequest(new { error = "Pénznem kiválasztása kötelező." });
        }
        if (dto.QuoteItems == null || !dto.QuoteItems.Any())
        {
            _logger.LogWarning("No quote items provided");
            return BadRequest(new { error = "Legalább egy tételt hozzá kell adni." });
        }

        // Validate discounts
        if (dto.DiscountPercentage.HasValue && dto.DiscountPercentage > 0 && dto.DiscountAmount.HasValue && dto.DiscountAmount > 0)
        {
            _logger.LogWarning("Both DiscountPercentage and DiscountAmount provided for quote");
            return BadRequest(new { error = "Csak kedvezmény százaléka vagy összege adható meg, nem mindkettő." });
        }

        foreach (var item in dto.QuoteItems)
        {
            if (item.ProductId <= 0)
            {
                _logger.LogWarning("Invalid ProductId: {ProductId}", item.ProductId);
                return BadRequest(new { error = "Termék kiválasztása kötelező." });
            }
            if (item.Quantity <= 0)
            {
                _logger.LogWarning("Invalid Quantity: {Quantity}", item.Quantity);
                return BadRequest(new { error = "Mennyiség pozitív kell legyen." });
            }
            if (item.UnitPrice < 0)
            {
                _logger.LogWarning("Invalid UnitPrice: {UnitPrice}", item.UnitPrice);
                return BadRequest(new { error = "Egységár nem lehet negatív." });
            }
            if (item.DiscountPercentage.HasValue && item.DiscountAmount.HasValue && item.DiscountPercentage > 0 && item.DiscountAmount > 0)
            {
                _logger.LogWarning("Both DiscountPercentage and DiscountAmount provided for item: ProductId={ProductId}", item.ProductId);
                return BadRequest(new { error = "Tételenként csak kedvezmény százaléka vagy összege adható meg, nem mindkettő." });
            }
            if (item.DiscountPercentage.HasValue && (item.DiscountPercentage < 0 || item.DiscountPercentage > 100))
            {
                _logger.LogWarning("Invalid DiscountPercentage: {DiscountPercentage}", item.DiscountPercentage);
                return BadRequest(new { error = "Kedvezmény százaléka 0 és 100 között kell legyen." });
            }
            if (item.DiscountAmount.HasValue && item.DiscountAmount < 0)
            {
                _logger.LogWarning("Invalid DiscountAmount: {DiscountAmount}", item.DiscountAmount);
                return BadRequest(new { error = "Kedvezmény összege nem lehet negatív." });
            }

            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
            {
                _logger.LogError("Product not found: ProductId={ProductId}", item.ProductId);
                return BadRequest(new { error = "A kiválasztott termék nem található." });
            }
        }

        Quote quote = null;
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (dto.QuoteId > 0)
            {
                // Update existing quote
                quote = await _context.Quotes
                    .Include(q => q.QuoteItems)
                    .FirstOrDefaultAsync(q => q.QuoteId == dto.QuoteId);

                if (quote == null)
                {
                    _logger.LogError("Quote not found: QuoteId={QuoteId}", dto.QuoteId);
                    return BadRequest(new { error = "Árajánlat nem található." });
                }

                // Update quote fields
                quote.QuoteDate = dto.QuoteDate ?? quote.QuoteDate;
                quote.PartnerId = dto.PartnerId;
                quote.CurrencyId = dto.CurrencyId;
                quote.SalesPerson = dto.SalesPerson ?? quote.SalesPerson ?? "System";
                quote.ValidityDate = dto.ValidityDate ?? quote.ValidityDate;
                quote.Status = dto.Status ?? quote.Status ?? "Draft";
                quote.Subject = dto.Subject;
                quote.Description = dto.Description;
                quote.DetailedDescription = dto.DetailedDescription;
                quote.DiscountPercentage = dto.DiscountPercentage > 0 ? dto.DiscountPercentage : null;
                quote.DiscountAmount = dto.DiscountAmount > 0 ? dto.DiscountAmount : null;
                quote.ModifiedBy = User.Identity?.Name ?? "System";
                quote.ModifiedDate = DateTime.UtcNow;

                // Clear existing items and add new ones
                quote.QuoteItems.Clear();
                foreach (var item in dto.QuoteItems)
                {
                    quote.QuoteItems.Add(new QuoteItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        ItemDescription = item.ItemDescription ?? "Nincs leírás",
                        DiscountPercentage = item.DiscountPercentage,
                        DiscountAmount = item.DiscountAmount
                    });
                }
            }
            else
            {
                // Create new quote
                quote = new Quote
                {
                    PartnerId = dto.PartnerId,
                    CurrencyId = dto.CurrencyId,
                    QuoteDate = dto.QuoteDate ?? DateTime.UtcNow,
                    SalesPerson = dto.SalesPerson ?? "System",
                    ValidityDate = dto.ValidityDate,
                    Status = dto.Status ?? "Draft",
                    Subject = dto.Subject,
                    Description = dto.Description,
                    DetailedDescription = dto.DetailedDescription,
                    DiscountPercentage = dto.DiscountPercentage > 0 ? dto.DiscountPercentage : null,
                    DiscountAmount = dto.DiscountAmount > 0 ? dto.DiscountAmount : null,
                    CreatedBy = User.Identity?.Name ?? "System",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedBy = User.Identity?.Name ?? "System",
                    ModifiedDate = DateTime.UtcNow,
                    QuoteItems = new List<QuoteItem>()
                };

                // Generate QuoteNumber
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
                quote.QuoteNumber = nextNumber;

                // Add QuoteItems
                foreach (var item in dto.QuoteItems)
                {
                    quote.QuoteItems.Add(new QuoteItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        ItemDescription = item.ItemDescription ?? "Nincs leírás",
                        DiscountPercentage = item.DiscountPercentage,
                        DiscountAmount = item.DiscountAmount
                    });
                }
            }

            // Calculate TotalAmount
            decimal itemsTotal = quote.QuoteItems.Sum(qi =>
            {
                decimal baseTotal = qi.Quantity * qi.UnitPrice;
                decimal discount = qi.DiscountAmount ?? (qi.DiscountPercentage.HasValue ? (baseTotal * qi.DiscountPercentage.Value / 100) : 0);
                return baseTotal - discount;
            });

            if (quote.DiscountAmount.HasValue && quote.DiscountAmount > 0)
            {
                quote.DiscountPercentage = null;
                quote.TotalAmount = itemsTotal - quote.DiscountAmount.Value;
            }
            else if (quote.DiscountPercentage.HasValue && quote.DiscountPercentage > 0)
            {
                quote.DiscountAmount = itemsTotal * quote.DiscountPercentage.Value / 100;
                quote.TotalAmount = itemsTotal - quote.DiscountAmount.Value;
            }
            else
            {
                quote.DiscountAmount = null;
                quote.DiscountPercentage = null;
                quote.TotalAmount = itemsTotal;
            }

            if (dto.QuoteId == 0)
            {
                _context.Quotes.Add(quote);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Quote saved: QuoteId={QuoteId}, QuoteNumber={QuoteNumber}, ItemsCount={ItemsCount}", quote.QuoteId, quote.QuoteNumber, quote.QuoteItems.Count);
            return new JsonResult(new { quoteId = quote.QuoteId });
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to save quote: {Message}", ex.InnerException?.Message);
            return StatusCode(500, new { error = "Hiba történt az árajánlat mentése közben: lehetséges duplikált árajánlat szám vagy érvénytelen adat." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing quote: {Message}", ex.Message);
            return StatusCode(500, new { error = "Hiba történt az árajánlat mentése közben." });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error processing quote: {Message}", ex.Message);
        return StatusCode(500, new { error = "Váratlan hiba történt az árajánlat feldolgozása közben." });
    }
}

public async Task<IActionResult> OnPostAddItemAsync([FromBody] QuoteItemDto item)
{
    if (item.QuoteId <= 0 || item.ProductId <= 0 || item.Quantity <= 0 || item.UnitPrice < 0)
    {
        _logger.LogError("Invalid add data: QuoteId={QuoteId}, ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}", 
            item.QuoteId, item.ProductId, item.Quantity, item.UnitPrice);
        return BadRequest(new { success = false, message = "Érvénytelen tétel adatok." });
    }

    if (item.DiscountPercentage.HasValue && item.DiscountAmount.HasValue && item.DiscountPercentage > 0 && item.DiscountAmount > 0)
    {
        _logger.LogError("Both DiscountPercentage and DiscountAmount provided for item: ProductId={ProductId}", item.ProductId);
        return BadRequest(new { success = false, message = "Csak kedvezmény százaléka vagy összege adható meg, nem mindkettő." });
    }

    if (item.DiscountPercentage.HasValue && (item.DiscountPercentage < 0 || item.DiscountPercentage > 100))
    {
        _logger.LogError("Invalid DiscountPercentage: {DiscountPercentage}", item.DiscountPercentage);
        return BadRequest(new { success = false, message = "Kedvezmény százaléka 0 és 100 között kell legyen." });
    }

    if (item.DiscountAmount.HasValue && item.DiscountAmount < 0)
    {
        _logger.LogError("Invalid DiscountAmount: {DiscountAmount}", item.DiscountAmount);
        return BadRequest(new { success = false, message = "Kedvezmény összege nem lehet negatív." });
    }

    var quote = await _context.Quotes
        .Include(q => q.QuoteItems)
        .FirstOrDefaultAsync(q => q.QuoteId == item.QuoteId);

    if (quote == null)
    {
        _logger.LogError("Quote not found: QuoteId={QuoteId}", item.QuoteId);
        return BadRequest(new { success = false, message = "Árajánlat nem található." });
    }

    var product = await _context.Products.FindAsync(item.ProductId);
    if (product == null)
    {
        _logger.LogError("Product not found: ProductId={ProductId}", item.ProductId);
        return BadRequest(new { success = false, message = "A kiválasztott termék nem található." });
    }

    var quoteItem = new QuoteItem
    {
        QuoteId = item.QuoteId,
        ProductId = item.ProductId,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        ItemDescription = item.ItemDescription ?? "",
        DiscountPercentage = item.DiscountPercentage,
        DiscountAmount = item.DiscountAmount
    };

    _context.QuoteItems.Add(quoteItem);
    quote.ModifiedDate = DateTime.UtcNow;
    quote.ModifiedBy = User.Identity?.Name ?? "System";

    // Compute discounted total for all items
    decimal itemsTotal = quote.QuoteItems.Sum(qi =>
    {
        decimal baseTotal = qi.Quantity * qi.UnitPrice;
        decimal discount = qi.DiscountAmount ?? (qi.DiscountPercentage.HasValue ? (baseTotal * qi.DiscountPercentage.Value / 100) : 0);
        return baseTotal - discount;
    });

    // Add the new item's discounted total
    decimal newItemBaseTotal = item.Quantity * item.UnitPrice;
    decimal newItemDiscount = item.DiscountAmount ?? (item.DiscountPercentage.HasValue ? (newItemBaseTotal * item.DiscountPercentage.Value / 100) : 0);
    itemsTotal += (newItemBaseTotal - newItemDiscount);

    // Apply quote-level discount
    if (quote.DiscountPercentage.HasValue && quote.DiscountPercentage > 0)
    {
        quote.DiscountAmount = itemsTotal * quote.DiscountPercentage.Value / 100;
        quote.TotalAmount = itemsTotal - quote.DiscountAmount.Value;
    }
    else if (quote.DiscountAmount.HasValue && quote.DiscountAmount > 0)
    {
        quote.DiscountPercentage = null;
        quote.TotalAmount = itemsTotal - quote.DiscountAmount.Value;
    }
    else
    {
        quote.DiscountAmount = null;
        quote.DiscountPercentage = null;
        quote.TotalAmount = itemsTotal;
    }

    await _context.SaveChangesAsync();

    _logger.LogInformation("QuoteItem added: QuoteItemId={QuoteItemId}, QuoteId={QuoteId}", quoteItem.QuoteItemId, item.QuoteId);
    return new JsonResult(new { success = true, quoteItemId = quoteItem.QuoteItemId });
}

        public async Task<IActionResult> OnPostDeleteItemAsync(int quoteId, int quoteItemId)
        {
            if (quoteId <= 0 || quoteItemId <= 0)
            {
                _logger.LogError("Invalid delete data: QuoteId={QuoteId}, QuoteItemId={QuoteItemId}", quoteId, quoteItemId);
                return BadRequest(new { success = false, message = "Érvénytelen tétel adatok." });
            }

            var quoteItem = await _context.QuoteItems
                .Include(qi => qi.Quote)
                .ThenInclude(q => q.QuoteItems)
                .FirstOrDefaultAsync(qi => qi.QuoteItemId == quoteItemId && qi.QuoteId == quoteId);

            if (quoteItem == null)
            {
                _logger.LogError("QuoteItem not found: QuoteItemId={QuoteItemId}, QuoteId={QuoteId}", quoteItemId, quoteId);
                return BadRequest(new { success = false, message = "Tétel nem található vagy nem tartozik az árajánlathoz." });
            }

            _context.QuoteItems.Remove(quoteItem);
            quoteItem.Quote.ModifiedDate = DateTime.UtcNow;
            quoteItem.Quote.ModifiedBy = User.Identity?.Name ?? "System";

            // Compute discounted total for remaining items
            decimal itemsTotal = quoteItem.Quote.QuoteItems.Sum(qi =>
            {
                decimal baseTotal = qi.Quantity * qi.UnitPrice;
                decimal discount = qi.DiscountAmount ?? (qi.DiscountPercentage.HasValue ? (baseTotal * qi.DiscountPercentage.Value / 100) : 0);
                return baseTotal - discount;
            });

            // Apply quote-level discount
            quoteItem.Quote.DiscountAmount = (quoteItem.Quote.DiscountPercentage > 0) ? (itemsTotal * quoteItem.Quote.DiscountPercentage.Value / 100) : (quoteItem.Quote.DiscountAmount ?? 0);
            quoteItem.Quote.TotalAmount = itemsTotal - (quoteItem.Quote.DiscountAmount ?? 0);

            await _context.SaveChangesAsync();

            _logger.LogInformation("QuoteItem deleted: QuoteItemId={QuoteItemId}, QuoteId={QuoteId}", quoteItemId, quoteId);
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostUpdateItemAsync(int quoteItemId, int quoteId, int productId, decimal quantity, decimal unitPrice, string itemDescription, decimal? discountPercentage, decimal? discountAmount)
        {
            if (quoteItemId <= 0 || quoteId <= 0 || productId <= 0 || quantity <= 0 || unitPrice < 0)
            {
                _logger.LogError("Invalid update data: QuoteItemId={QuoteItemId}, QuoteId={QuoteId}, ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}", quoteItemId, quoteId, productId, quantity, unitPrice);
                return BadRequest(new { success = false, message = "Érvénytelen tétel adatok." });
            }

            var quoteItem = await _context.QuoteItems
                .Include(qi => qi.Quote)
                .ThenInclude(q => q.QuoteItems)
                .FirstOrDefaultAsync(qi => qi.QuoteItemId == quoteItemId && qi.QuoteId == quoteId);

            if (quoteItem == null)
            {
                _logger.LogError("QuoteItem not found: QuoteItemId={QuoteItemId}, QuoteId={QuoteId}", quoteItemId, quoteId);
                return BadRequest(new { success = false, message = "Tétel nem található vagy nem tartozik az árajánlathoz." });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                _logger.LogError("Product not found: ProductId={ProductId}", productId);
                return BadRequest(new { success = false, message = "A kiválasztott termék nem található." });
            }

            quoteItem.ProductId = productId;
            quoteItem.Quantity = quantity;
            quoteItem.UnitPrice = unitPrice;
            quoteItem.ItemDescription = itemDescription ?? "";
            quoteItem.DiscountPercentage = discountPercentage > 0 ? discountPercentage : null;
            quoteItem.DiscountAmount = discountAmount > 0 ? discountAmount : null;

            quoteItem.Quote.ModifiedDate = DateTime.UtcNow;
            quoteItem.Quote.ModifiedBy = User.Identity?.Name ?? "System";

            // Compute discounted total for all items
            decimal itemsTotal = quoteItem.Quote.QuoteItems.Sum(qi =>
            {
                decimal baseTotal = qi.Quantity * qi.UnitPrice;
                decimal discount = qi.DiscountAmount ?? (qi.DiscountPercentage.HasValue ? (baseTotal * qi.DiscountPercentage.Value / 100) : 0);
                return baseTotal - discount;
            });

            // Apply quote-level discount
            quoteItem.Quote.DiscountAmount = (quoteItem.Quote.DiscountPercentage > 0) ? (itemsTotal * quoteItem.Quote.DiscountPercentage.Value / 100) : (quoteItem.Quote.DiscountAmount ?? 0);
            quoteItem.Quote.TotalAmount = itemsTotal - (quoteItem.Quote.DiscountAmount ?? 0);

            await _context.SaveChangesAsync();

            _logger.LogInformation("QuoteItem updated: QuoteItemId={QuoteItemId}, QuoteId={QuoteId}", quoteItemId, quoteId);
            return new JsonResult(new { success = true });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditQuote(int quoteId, int partnerId, string salesPerson, string quoteNumber, DateTime quoteDate, DateTime validityDate, string status, string subject, string detailedDescription, string? description)
        {
            try
            {
                _logger.LogInformation("Editing quote ID: {QuoteId}, PartnerId: {PartnerId}, SalesPerson: {SalesPerson}", quoteId, partnerId, salesPerson);

                var quote = await _context.Quotes
                    .Include(q => q.Partner)
                    .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

                if (quote == null)
                {
                    _logger.LogWarning("Quote not found: {QuoteId}", quoteId);
                    return new JsonResult(new { success = false, message = "Quote not found." });
                }

                var partner = await _context.Partners.FindAsync(partnerId);
                if (partner == null)
                {
                    _logger.LogWarning("Partner not found: {PartnerId}", partnerId);
                    return new JsonResult(new { success = false, message = "Selected partner does not exist." });
                }

                if (string.IsNullOrWhiteSpace(quoteNumber) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(status) || string.IsNullOrWhiteSpace(salesPerson))
                {
                    _logger.LogWarning("Required fields missing for quote ID: {QuoteId}", quoteId);
                    return new JsonResult(new { success = false, message = "Required fields are missing." });
                }

                quote.PartnerId = partnerId;
                quote.SalesPerson = salesPerson;
                quote.QuoteNumber = quoteNumber;
                quote.Subject = subject;
                quote.Description = description;
                quote.DetailedDescription = detailedDescription;
                quote.QuoteDate = quoteDate;
                quote.ValidityDate = validityDate;
                quote.Status = status;
                quote.Subject = subject;
                quote.Description = description;
                quote.ModifiedBy = User.Identity?.Name ?? "System";
                quote.ModifiedDate = DateTime.UtcNow;

                _context.Quotes.Update(quote);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Quote ID: {QuoteId} updated successfully", quoteId);
                return new JsonResult(new { success = true, quoteNumber = quote.QuoteNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing quote ID: {QuoteId}", quoteId);
                return new JsonResult(new { success = false, message = $"Error saving quote: {ex.Message}" });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteQuote(int quoteId)
        {
            try
            {
                var quote = await _context.Quotes
                    .Include(q => q.QuoteItems)
                    .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

                if (quote == null)
                {
                    return new JsonResult(new { success = false, message = "Quote not found." });
                }

                // Delete associated QuoteItems
                _context.QuoteItems.RemoveRange(quote.QuoteItems);

                // Delete the quote
                _context.Quotes.Remove(quote);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (DbUpdateException ex)
            {
                return new JsonResult(new { success = false, message = "Database error: Unable to delete quote due to related records." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error deleting quote: {ex.Message}" });
            }
        }
    }
}