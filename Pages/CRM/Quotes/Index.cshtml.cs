using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.CRM.Quotes
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly QuoteService _quoteService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, QuoteService quoteService, ILogger<IndexModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IList<Quote> Quotes { get; set; } = new List<Quote>();
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public int PageSize { get; set; } = 10;
        public string NextQuoteNumber { get; set; }
        public QuoteStatus Status { get; set; }
        public QuoteDto Quote { get; set; }
        public string StatusFilter { get; set; }
        public string SortBy { get; set; }

        [BindProperty]
        public int PartnerId { get; set; }
        public IEnumerable<SelectListItem> Partners { get; set; } = new List<SelectListItem>();

        [BindProperty]
        public int CurrencyId { get; set; }
        public IEnumerable<SelectListItem> Currencies { get; set; } = new List<SelectListItem>();

        public IEnumerable<SelectListItem> VatTypes { get; set; } = new List<SelectListItem>();

        public async Task OnGetAsync(int? pageNumber, string searchTerm, int? pageSize, string statusFilter, string sortBy)
        {
            CurrentPage = pageNumber ?? 1;
            SearchTerm = searchTerm;
            PageSize = pageSize ?? 10;
            StatusFilter = statusFilter;
            SortBy = sortBy;

            Partners = await _context.Partners
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.PartnerId.ToString(),
                    Text = p.TaxId != null ? $"{p.Name} ({p.TaxId})" : p.Name
                })
                .ToListAsync();

            Currencies = await _context.Currencies
                .OrderBy(c => c.CurrencyName)
                .Select(c => new SelectListItem
                {
                    Value = c.CurrencyId.ToString(),
                    Text = c.CurrencyName
                })
                .ToListAsync();

            VatTypes = await _context.VatTypes
                .OrderBy(v => v.Rate)
                .Select(v => new SelectListItem
                {
                    Value = v.VatTypeId.ToString(),
                    Text = v.FormattedRate
                })
                .ToListAsync();

            _logger.LogInformation("Fetching quotes: Page={Page}, PageSize={PageSize}, SearchTerm={SearchTerm}, StatusFilter={StatusFilter}, SortBy={SortBy}",
                CurrentPage, PageSize, SearchTerm, StatusFilter, SortBy);

            IQueryable<Quote> quotesQuery = _context.Quotes
                .Include(q => q.Partner)
                .Include(q => q.Currency)
                .Include(q => q.QuoteItems)
                    .ThenInclude(qi => qi.Product)
                .Include(q => q.QuoteItems)
                    .ThenInclude(qi => qi.VatType);

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                quotesQuery = quotesQuery.Where(q => q.QuoteNumber.Contains(SearchTerm) ||
                                                    q.Subject.Contains(SearchTerm) ||
                                                    q.Currency.CurrencyName.Contains(SearchTerm) ||
                                                    q.Partner.Name.Contains(SearchTerm) ||
                                                    q.Description.Contains(SearchTerm));
            }

            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "all")
            {
                quotesQuery = quotesQuery.Where(q => q.Status == StatusFilter);
            }

            quotesQuery = SortBy switch
            {
                "QuoteId" => quotesQuery.OrderByDescending(q => q.QuoteId),
                "ValidityDate" => quotesQuery.OrderBy(q => q.ValidityDate).ThenByDescending(q => q.QuoteId),
                "QuoteDate" => quotesQuery.OrderByDescending(q => q.QuoteDate).ThenByDescending(q => q.QuoteId),
                _ => quotesQuery.OrderByDescending(q => q.QuoteDate).ThenByDescending(q => q.QuoteId)
            };

            TotalRecords = await quotesQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages));

            Quotes = await quotesQuery
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Generate NextQuoteNumber with validation
            var lastQuote = await _context.Quotes
                .OrderByDescending(q => q.QuoteId)
                .FirstOrDefaultAsync();

            if (lastQuote != null && !string.IsNullOrEmpty(lastQuote.QuoteNumber))
            {
                var match = Regex.Match(lastQuote.QuoteNumber, @"^Q(\d+)$");
                if (match.Success && long.TryParse(match.Groups[1].Value, out var lastNumber))
                {
                    NextQuoteNumber = $"Q{(lastNumber + 1).ToString("D6")}";
                }
                else
                {
                    _logger.LogWarning("Invalid QuoteNumber format for last quote: {QuoteNumber}. Using default.", lastQuote.QuoteNumber);
                    NextQuoteNumber = "Q000001";
                }
            }
            else
            {
                NextQuoteNumber = "Q000001";
            }

            _logger.LogInformation("Retrieved {Count} quotes for page {Page}. TotalRecords={TotalRecords}, TotalPages={TotalPages}, StatusFilter={StatusFilter}, SortBy={SortBy}, NextQuoteNumber={NextQuoteNumber}",
                Quotes.Count, CurrentPage, TotalRecords, TotalPages, StatusFilter, SortBy, NextQuoteNumber);

            if (!Quotes.Any() && TotalRecords > 0)
            {
                _logger.LogWarning("No quotes found for page {Page}, but TotalRecords={TotalRecords}. Possible pagination or filter issue.", CurrentPage, TotalRecords);
            }
        }

        public async Task<IActionResult> OnPostCreateQuoteAsync([FromBody] CreateQuoteDto createQuoteDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("ModelState errors: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, message = "Érvénytelen adatok", errors });
            }

            if (createQuoteDto.PartnerId <= 0)
            {
                return BadRequest(new { success = false, message = "Érvénytelen partner azonosító." });
            }

            if (createQuoteDto.CurrencyId <= 0)
            {
                return BadRequest(new { success = false, message = "Érvénytelen pénznem azonosító." });
            }

            if (string.IsNullOrEmpty(createQuoteDto.Subject?.Trim()))
            {
                return BadRequest(new { success = false, message = "A tárgy mező kitöltése kötelező." });
            }

            if (createQuoteDto.QuoteItems == null || !createQuoteDto.QuoteItems.Any(i => i.ProductId > 0))
            {
                return BadRequest(new { success = false, message = "Legalább egy érvényes tétel szükséges (termék megadása kötelező)." });
            }

            foreach (var item in createQuoteDto.QuoteItems)
            {
                if (item.VatTypeId <= 0)
                {
                    return BadRequest(new { success = false, message = "Minden tételhez szükséges ÁFA típus." });
                }
                if (item.Quantity <= 0)
                {
                    return BadRequest(new { success = false, message = "A mennyiségnek pozitívnak kell lennie minden tételhez." });
                }
                if (item.ListPrice < 0)
                {
                    return BadRequest(new { success = false, message = "A listaár nem lehet negatív." });
                }
                if (item.NetDiscountedPrice < 0)
                {
                    return BadRequest(new { success = false, message = "A nettó kedvezményes ár nem lehet negatív." });
                }
                if (item.TotalPrice < 0)
                {
                    return BadRequest(new { success = false, message = "Az összes ár nem lehet negatív." });
                }
                if (item.DiscountTypeId.HasValue && !Enum.IsDefined(typeof(DiscountType), item.DiscountTypeId.Value))
                {
                    return BadRequest(new { success = false, message = $"Érvénytelen kedvezmény típus a tételhez: {item.ProductId}" });
                }
                if (item.DiscountTypeId == 5 && (item.DiscountAmount < 0 || item.DiscountAmount > item.ListPrice * item.Quantity))
                {
                    return BadRequest(new { success = false, message = "A kedvezmény százaléknak megfelelő összeg nem lehet negatív vagy nagyobb az összes árnál." });
                }
                if (item.DiscountTypeId == 6 && (item.DiscountAmount < 0 || item.DiscountAmount >= item.ListPrice))
                {
                    return BadRequest(new { success = false, message = "A kedvezmény összeg nem lehet negatív vagy nagyobb/egyenlő a listaárral." });
                }
            }

            try
            {
                if (string.IsNullOrEmpty(createQuoteDto.QuoteNumber))
                {
                    var lastQuote = await _context.Quotes
                        .OrderByDescending(q => q.QuoteId)
                        .FirstOrDefaultAsync();
                    if (lastQuote != null && !string.IsNullOrEmpty(lastQuote.QuoteNumber))
                    {
                        var match = Regex.Match(lastQuote.QuoteNumber, @"^Q(\d+)$");
                        if (match.Success && long.TryParse(match.Groups[1].Value, out var lastNumber))
                        {
                            createQuoteDto.QuoteNumber = $"Q{(lastNumber + 1).ToString("D6")}";
                        }
                        else
                        {
                            createQuoteDto.QuoteNumber = "Q000001";
                        }
                    }
                    else
                    {
                        createQuoteDto.QuoteNumber = "Q000001";
                    }
                }

                // Get the current user's username
                string createdBy = User.Identity?.Name ?? "System";

                // Pass the username to the QuoteService
                var quote = await _quoteService.CreateQuoteAsync(createQuoteDto, createdBy);
                await LogHistoryAsync(quote.QuoteId, "Created", null, null, null, $"Árajánlat létrehozva: {quote.QuoteNumber}");

                return new JsonResult(new
                {
                    success = true,
                    message = "Árajánlat sikeresen létrehozva",
                    quoteId = quote.QuoteId,
                    quoteNumber = quote.QuoteNumber
                });
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogWarning(ioEx, "Validation error creating quote: {Message}", ioEx.Message);
                return BadRequest(new { success = false, message = ioEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quote: {Message}. Inner: {InnerMessage}", ex.Message, ex.InnerException?.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Hiba történt az árajánlat létrehozása közben: {ex.Message}. Részletek: {ex.InnerException?.Message}"
                });
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

        private List<(string FieldName, string OldValue, string NewValue)> DetectChanges(Quote oldQuote, Quote newQuote)
        {
            var changes = new List<(string FieldName, string OldValue, string NewValue)>();

            if (oldQuote.QuoteNumber != newQuote.QuoteNumber)
                changes.Add(("QuoteNumber", oldQuote.QuoteNumber, newQuote.QuoteNumber));

            if (oldQuote.QuoteDate != newQuote.QuoteDate)
                changes.Add(("QuoteDate", oldQuote.QuoteDate?.ToString("o"), newQuote.QuoteDate?.ToString("o")));

            if (oldQuote.Description != newQuote.Description)
                changes.Add(("Description", oldQuote.Description, newQuote.Description));

            if (oldQuote.TotalAmount != newQuote.TotalAmount)
                changes.Add(("TotalAmount", oldQuote.TotalAmount?.ToString("F2"), newQuote.TotalAmount?.ToString("F2")));

            if (oldQuote.SalesPerson != newQuote.SalesPerson)
                changes.Add(("SalesPerson", oldQuote.SalesPerson, newQuote.SalesPerson));

            if (oldQuote.ValidityDate != newQuote.ValidityDate)
                changes.Add(("ValidityDate", oldQuote.ValidityDate?.ToString("o"), newQuote.ValidityDate?.ToString("o")));

            if (oldQuote.DiscountPercentage != newQuote.DiscountPercentage)
                changes.Add(("DiscountPercentage", oldQuote.DiscountPercentage?.ToString("F2"), newQuote.DiscountPercentage?.ToString("F2")));

            if (oldQuote.CompanyName != newQuote.CompanyName)
                changes.Add(("CompanyName", oldQuote.CompanyName, newQuote.CompanyName));

            if (oldQuote.Subject != newQuote.Subject)
                changes.Add(("Subject", oldQuote.Subject, newQuote.Subject));

            if (oldQuote.DetailedDescription != newQuote.DetailedDescription)
                changes.Add(("DetailedDescription", oldQuote.DetailedDescription, newQuote.DetailedDescription));

            if (oldQuote.Status != newQuote.Status)
                changes.Add(("Status", oldQuote.Status, newQuote.Status));

            if (oldQuote.PartnerId != newQuote.PartnerId)
                changes.Add(("PartnerId", oldQuote.PartnerId.ToString(), newQuote.PartnerId.ToString()));

            if (oldQuote.CurrencyId != newQuote.CurrencyId)
                changes.Add(("CurrencyId", oldQuote.CurrencyId.ToString(), newQuote.CurrencyId.ToString()));

            return changes;
        }

        private async Task LogHistoryAsync(int quoteId, string action, string? fieldName, string? oldValue, string? newValue, string comment)
        {
            // Get the current user's username
            string modifiedBy = User.Identity?.Name ?? "System";

            var history = new QuoteHistory
            {
                QuoteId = quoteId,
                Action = action,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                ModifiedBy = modifiedBy,
                ModifiedDate = DateTime.UtcNow,
                Comment = comment
            };

            _context.QuoteHistories.Add(history);
            await _context.SaveChangesAsync();
        }
    }

    public enum QuoteStatus
    {
        Folyamatban,
        Felfüggesztve,
        Jóváhagyásra_vár,
        Jóváhagyva,
        Kiküldve,
        Elfogadva,
        Megrendelve,
        Teljesítve,
        Lezárva
    }
}