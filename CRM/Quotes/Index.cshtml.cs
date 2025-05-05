using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
                        .Select(q => new
                        {
                            q.QuoteId,
                            q.QuoteNumber,
                            q.Status,
                            PartnerName = q.Partner != null ? q.Partner.Name : null,
                            CurrencyName = q.Currency != null ? q.Currency.CurrencyName : null,
                            QuoteItemsCount = q.QuoteItems.Count
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
                    Quotes.Select(q => new
                    {
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

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditQuoteAsync()
        {
            var form = Request.Form;
            if (!int.TryParse(form["QuoteId"], out int quoteId) || quoteId <= 0)
            {
                _logger.LogWarning("Invalid QuoteId: {QuoteId}", form["QuoteId"]);
                return BadRequest(new { success = false, message = "Érvénytelen árajánlat azonosító." });
            }

            // Validate required fields
            if (!form.ContainsKey("QuoteDate") || string.IsNullOrEmpty(form["QuoteDate"]) ||
                !form.ContainsKey("PartnerId") || string.IsNullOrEmpty(form["PartnerId"]))
            {
                _logger.LogWarning("Required fields missing for QuoteId: {QuoteId}", quoteId);
                return BadRequest(new { success = false, message = "Kötelező mezők hiányoznak." });
            }

            if (!int.TryParse(form["PartnerId"], out int partnerId) || partnerId <= 0)
            {
                _logger.LogWarning("Invalid PartnerId for QuoteId: {QuoteId}, Value: {PartnerId}", quoteId, form["PartnerId"]);
                return BadRequest(new { success = false, message = "Érvénytelen partner azonosító." });
            }

            var quote = await _context.Quotes
                .Include(q => q.Partner)
                .Include(q => q.QuoteItems)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (quote == null)
            {
                _logger.LogWarning("Quote not found: QuoteId={QuoteId}", quoteId);
                return NotFound(new { success = false, message = "Árajánlat nem található." });
            }

            var partner = await _context.Partners.FindAsync(partnerId);
            if (partner == null)
            {
                _logger.LogWarning("Partner not found: PartnerId={PartnerId} for QuoteId={QuoteId}", partnerId, quoteId);
                return BadRequest(new { success = false, message = "Érvénytelen partner azonosító." });
            }

            try
            {
                // Update quote fields
                quote.QuoteNumber = form["QuoteNumber"].ToString();
                if (DateTime.TryParse(form["QuoteDate"], out var quoteDate))
                    quote.QuoteDate = quoteDate;
                else
                    return BadRequest(new { success = false, message = "Érvénytelen dátum formátum." });

                quote.PartnerId = partnerId;
                quote.SalesPerson = form["SalesPerson"].ToString();
                if (DateTime.TryParse(form["ValidityDate"], out var validityDate))
                    quote.ValidityDate = validityDate;
                else
                    quote.ValidityDate = null;

                quote.Status = form["Status"].Count > 0 ? form["Status"].ToString() : "Draft";
                quote.Subject = form["Subject"].ToString();
                quote.Description = form["Description"].ToString();
                quote.DetailedDescription = form["DetailedDescription"].ToString();

                if (decimal.TryParse(form["DiscountPercentage"], NumberStyles.Any, CultureInfo.InvariantCulture, out var discountPercentage))
                    quote.DiscountPercentage = discountPercentage > 0 ? discountPercentage : null;
                else
                    quote.DiscountPercentage = null;

                if (decimal.TryParse(form["DiscountAmount"], NumberStyles.Any, CultureInfo.InvariantCulture, out var discountAmount))
                    quote.DiscountAmount = discountAmount > 0 ? discountAmount : null;
                else
                    quote.DiscountAmount = null;

                // Recalculate TotalAmount
                decimal itemsTotal = quote.QuoteItems.Sum(qi =>
                {
                    decimal baseTotal = qi.Quantity * qi.UnitPrice;
                    decimal discount = qi.DiscountAmount ?? (qi.DiscountPercentage.HasValue ? (baseTotal * qi.DiscountPercentage.Value / 100) : 0);
                    return baseTotal - discount;
                });

                decimal quoteDiscount = quote.DiscountAmount ?? (quote.DiscountPercentage.HasValue ? (itemsTotal * quote.DiscountPercentage.Value / 100) : 0);
                quote.TotalAmount = itemsTotal - quoteDiscount;

                quote.ModifiedDate = DateTime.UtcNow;
                quote.ModifiedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();
                _logger.LogInformation("Quote updated: QuoteId={QuoteId}, QuoteNumber={QuoteNumber}, PartnerId={PartnerId}", quoteId, quote.QuoteNumber, partnerId);
                return new JsonResult(new { success = true });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to update quote: QuoteId={QuoteId}, Error={Message}", quoteId, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt az árajánlat frissítése közben: adatbázis hiba." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating quote: QuoteId={QuoteId}, Error={Message}", quoteId, ex.Message);
                return StatusCode(500, new { success = false, message = "Váratlan hiba történt az árajánlat frissítése közben." });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteQuoteAsync(int quoteId)
        {
            _logger.LogInformation("OnPostDeleteQuoteAsync called with QuoteId: {QuoteId}", quoteId);

            if (quoteId <= 0)
            {
                _logger.LogWarning("Invalid QuoteId: {QuoteId}", quoteId);
                return BadRequest(new { success = false, message = "Érvénytelen árajánlat azonosító." });
            }

            var quote = await _context.Quotes
                .Include(q => q.QuoteItems)
                .FirstOrDefaultAsync(q => q.QuoteId == quoteId);

            if (quote == null)
            {
                _logger.LogWarning("Quote not found: QuoteId={QuoteId}", quoteId);
                return NotFound(new { success = false, message = "Árajánlat nem található." });
            }

            try
            {
                _context.Quotes.Remove(quote);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Quote deleted: QuoteId={QuoteId}, QuoteNumber={QuoteNumber}, ItemsCount={ItemsCount}",
                    quote.QuoteId, quote.QuoteNumber, quote.QuoteItems?.Count ?? 0);
                return new JsonResult(new { success = true });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete quote: QuoteId={QuoteId}, Error={Message}", quoteId, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt az árajánlat törlése közben: adatbázis hiba." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting quote: QuoteId={QuoteId}, Error={Message}", quoteId, ex.Message);
                return StatusCode(500, new { success = false, message = "Váratlan hiba történt az árajánlat törlése közben." });
            }
        }

        public async Task<IActionResult> OnPostAddItemAsync([FromBody] QuoteItemDto item)
        {
            _logger.LogInformation("OnPostAddItemAsync called with item: {@Item}", item);

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

            try
            {
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
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to add quote item: QuoteId={QuoteId}, Error={Message}", item.QuoteId, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt a tétel hozzáadása közben: adatbázis hiba." });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUpdateItemAsync([FromBody] QuoteItemDto item)
        {
            _logger.LogInformation("OnPostUpdateItemAsync called with item: {@Item}", item);

            if (item.QuoteId <= 0 || item.QuoteItemId <= 0 || item.ProductId <= 0 || item.Quantity <= 0 || item.UnitPrice < 0)
            {
                _logger.LogError("Invalid update data: QuoteId={QuoteId}, QuoteItemId={QuoteItemId}, ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}",
                    item.QuoteId, item.QuoteItemId, item.ProductId, item.Quantity, item.UnitPrice);
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

            var quoteItem = quote.QuoteItems.FirstOrDefault(qi => qi.QuoteItemId == item.QuoteItemId);
            if (quoteItem == null)
            {
                _logger.LogError("QuoteItem not found: QuoteItemId={QuoteItemId}, QuoteId={QuoteId}", item.QuoteItemId, item.QuoteId);
                return BadRequest(new { success = false, message = "Tétel nem található vagy nem tartozik az árajánlathoz." });
            }

            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
            {
                _logger.LogError("Product not found: ProductId={ProductId}", item.ProductId);
                return BadRequest(new { success = false, message = "A kiválasztott termék nem található." });
            }

            try
            {
                quoteItem.ProductId = item.ProductId;
                quoteItem.Quantity = item.Quantity;
                quoteItem.UnitPrice = item.UnitPrice;
                quoteItem.ItemDescription = item.ItemDescription ?? "";
                quoteItem.DiscountPercentage = item.DiscountPercentage;
                quoteItem.DiscountAmount = item.DiscountAmount;

                quote.ModifiedDate = DateTime.UtcNow;
                quote.ModifiedBy = User.Identity?.Name ?? "System";

                // Compute discounted total for all items
                decimal itemsTotal = quote.QuoteItems.Sum(qi =>
                {
                    decimal baseTotal = qi.Quantity * qi.UnitPrice;
                    decimal discount = qi.DiscountAmount ?? (qi.DiscountPercentage.HasValue ? (baseTotal * qi.DiscountPercentage.Value / 100) : 0);
                    return baseTotal - discount;
                });

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
                _logger.LogInformation("QuoteItem updated: QuoteItemId={QuoteItemId}, QuoteId={QuoteId}", item.QuoteItemId, item.QuoteId);
                return new JsonResult(new { success = true });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to update quote item: QuoteId={QuoteId}, QuoteItemId={QuoteItemId}, Error={Message}", item.QuoteId, item.QuoteItemId, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt a tétel frissítése közben: adatbázis hiba." });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteItemAsync([FromBody] DeleteItemDto data)
        {
            _logger.LogInformation("OnPostDeleteItemAsync called with QuoteId={QuoteId}, QuoteItemId={QuoteItemId}", data.QuoteId, data.QuoteItemId);

            if (data.QuoteId <= 0 || data.QuoteItemId <= 0)
            {
                _logger.LogError("Invalid delete data: QuoteId={QuoteId}, QuoteItemId={QuoteItemId}", data.QuoteId, data.QuoteItemId);
                return BadRequest(new { success = false, message = "Érvénytelen tétel adatok." });
            }

            var quote = await _context.Quotes
                .Include(q => q.QuoteItems)
                .FirstOrDefaultAsync(q => q.QuoteId == data.QuoteId);

            if (quote == null)
            {
                _logger.LogError("Quote not found: QuoteId={QuoteId}", data.QuoteId);
                return BadRequest(new { success = false, message = "Árajánlat nem található." });
            }

            var quoteItem = quote.QuoteItems.FirstOrDefault(qi => qi.QuoteItemId == data.QuoteItemId);
            if (quoteItem == null)
            {
                _logger.LogError("QuoteItem not found: QuoteItemId={QuoteItemId}, QuoteId={QuoteId}", data.QuoteItemId, data.QuoteId);
                return BadRequest(new { success = false, message = "Tétel nem található vagy nem tartozik az árajánlathoz." });
            }

            try
            {
                _context.QuoteItems.Remove(quoteItem);
                quote.ModifiedDate = DateTime.UtcNow;
                quote.ModifiedBy = User.Identity?.Name ?? "System";

                // Compute discounted total for remaining items
                decimal itemsTotal = quote.QuoteItems
                    .Where(qi => qi.QuoteItemId != data.QuoteItemId)
                    .Sum(qi =>
                    {
                        decimal baseTotal = qi.Quantity * qi.UnitPrice;
                        decimal discount = qi.DiscountAmount ?? (qi.DiscountPercentage.HasValue ? (baseTotal * qi.DiscountPercentage.Value / 100) : 0);
                        return baseTotal - discount;
                    });

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
                _logger.LogInformation("QuoteItem deleted: QuoteItemId={QuoteItemId}, QuoteId={QuoteId}", data.QuoteItemId, data.QuoteId);
                return new JsonResult(new { success = true });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete quote item: QuoteId={QuoteId}, QuoteItemId={QuoteItemId}, Error={Message}", data.QuoteId, data.QuoteItemId, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt a tétel törlése közben: adatbázis hiba." });
            }
        }

        public async Task<IActionResult> OnGetProductsAsync(string search)
        {
            _logger.LogInformation("OnGetProductsAsync called with search: {Search}", search ?? "null");

            try
            {
                var products = await _context.Products
                    .Where(p => string.IsNullOrEmpty(search) || p.Name.Contains(search))
                    .Select(p => new { id = p.ProductId, name = p.Name })
                    .Take(10)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} products for search: {Search}", products.Count, search);
                return new JsonResult(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt a termékek lekérdezése közben." });
            }
        }

        public async Task<IActionResult> OnGetCurrenciesAsync(string search)
        {
            _logger.LogInformation("OnGetCurrenciesAsync called with search: {Search}", search ?? "null");

            try
            {
                var currencies = await _context.Currencies
                    .Where(c => string.IsNullOrEmpty(search) || c.CurrencyName.Contains(search))
                    .Select(c => new { id = c.CurrencyId, name = c.CurrencyName })
                    .Take(10)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} currencies for search: {Search}", currencies.Count, search);
                return new JsonResult(currencies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching currencies: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt a pénznemek lekérdezése közben." });
            }
        }

        public async Task<IActionResult> OnGetPartnersAsync(string search)
        {
            _logger.LogInformation("OnGetPartnersAsync called with search: {Search}", search ?? "null");

            try
            {
                var partners = await _context.Partners
                    .Where(p => string.IsNullOrEmpty(search) || p.Name.Contains(search))
                    .Select(p => new { id = p.PartnerId, name = p.Name })
                    .Take(10)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} partners for search: {Search}", partners.Count, search);
                return new JsonResult(partners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partners: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt a partnerek lekérdezése közben." });
            }
        }

        public async Task<IActionResult> OnGetQuoteItemsAsync(int quoteId)
        {
            _logger.LogInformation("OnGetQuoteItemsAsync called for QuoteId: {QuoteId}", quoteId);

            try
            {
                var quoteExists = await _context.Quotes.AnyAsync(q => q.QuoteId == quoteId);
                if (!quoteExists)
                {
                    _logger.LogWarning("Quote not found for QuoteId: {QuoteId}", quoteId);
                    return NotFound(new { message = "Árajánlat nem található." });
                }

                var quoteItems = await _context.QuoteItems
                    .Where(qi => qi.QuoteId == quoteId)
                    .Include(qi => qi.Product)
                    .Select(qi => new
                    {
                        quoteItemId = qi.QuoteItemId,
                        productName = qi.Product != null ? qi.Product.Name : "N/A",
                        itemDescription = qi.ItemDescription ?? "",
                        quantity = qi.Quantity,
                        unitPrice = qi.UnitPrice,
                        discountPercentage = qi.DiscountPercentage,
                        discountAmount = qi.DiscountAmount
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} items for QuoteId: {QuoteId}, Items: {@Items}", quoteItems.Count, quoteId, quoteItems);
                return new JsonResult(quoteItems) { ContentType = "application/json" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quote items for QuoteId: {QuoteId}", quoteId);
                return StatusCode(500, new { message = "Hiba történt a tételek lekérdezése közben: " + ex.Message });
            }
        }
        public async Task<IActionResult> OnGetNextQuoteNumberAsync()
        {
            _logger.LogInformation("OnGetNextQuoteNumberAsync called");

            try
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
                _logger.LogInformation("Generated next quote number: {NextQuoteNumber}", nextNumber);
                return new JsonResult(nextNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating next quote number: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt az árajánlat szám generálása közben." });
            }
        }
    }

    // DTOs
    public class QuoteItemCreateDto
    {
        public int QuoteId { get; set; }
        public int PartnerId { get; set; }
        public int CurrencyId { get; set; }
        public DateTime? QuoteDate { get; set; }
        public string SalesPerson { get; set; }
        public DateTime? ValidityDate { get; set; }
        public string Status { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string DetailedDescription { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public List<QuoteItemDto> QuoteItems { get; set; }
    }

    public class QuoteItemDto
    {
        public int QuoteId { get; set; }
        public int QuoteItemId { get; set; }
        public int ProductId { get; set; }
        public string ItemDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
    }

    public class DeleteItemDto
    {
        public int QuoteId { get; set; }
        public int QuoteItemId { get; set; }
    }
}