using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;

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



        public async Task OnGetAsync(int? pageNumber, string searchTerm, int? pageSize, string statusFilter, string sortBy)
        {
            CurrentPage = pageNumber ?? 1;
            SearchTerm = searchTerm;
            PageSize = pageSize ?? 10;
            StatusFilter = statusFilter;
            SortBy = sortBy;

            Partners = _context.Partners
                        .OrderBy(p => p.Name)
                        .Select(p => new SelectListItem
                        {
                            Value = p.PartnerId.ToString(),
                            Text = p.TaxId != null ? $"{p.Name} ({p.TaxId})" : p.Name
                        }).ToList();

            Currencies = _context.Currencies
                .OrderBy(c => c.CurrencyName)
                .Select(c => new SelectListItem
                {
                    Value = c.CurrencyId.ToString(),
                    Text = c.CurrencyName
                }).ToList();

            _logger.LogInformation("Fetching quotes: Page={Page}, PageSize={PageSize}, SearchTerm={SearchTerm}, StatusFilter={StatusFilter}, SortBy={SortBy}",
                CurrentPage, PageSize, SearchTerm, StatusFilter, SortBy);

            IQueryable<Quote> quotesQuery = _context.Quotes
                .Include(q => q.Partner)
                .Include(q => q.QuoteItems)
                .ThenInclude(qi => qi.Product)
                .Include(q => q.QuoteItems)
                    .ThenInclude(qi => qi.VatType); ;

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                quotesQuery = quotesQuery.Where(q => q.QuoteNumber.Contains(SearchTerm) ||
                                                    q.Subject.Contains(SearchTerm) ||
                                                    q.Partner.Name.Contains(SearchTerm) ||
                                                    q.Description.Contains(SearchTerm));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "all")
            {
                quotesQuery = quotesQuery.Where(q => q.Status == StatusFilter);
            }

            quotesQuery = SortBy switch
            {
                "QuoteId" => quotesQuery.OrderByDescending(q => q.QuoteId),
                "ValidityDate" => quotesQuery.OrderBy(q => q.ValidityDate).ThenByDescending(q => q.QuoteId),
                "QuoteDate" => quotesQuery.OrderByDescending(q => q.QuoteDate).ThenByDescending(q => q.QuoteId),
                _ => quotesQuery.OrderByDescending(q => q.QuoteDate).ThenByDescending(q => q.QuoteId) // Default
            };

            TotalRecords = await quotesQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages));

            Quotes = await quotesQuery
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} quotes for page {Page}. TotalRecords={TotalRecords}, TotalPages={TotalPages}, StatusFilter={StatusFilter}, SortBy={SortBy}",
                Quotes.Count, CurrentPage, TotalRecords, TotalPages, StatusFilter, SortBy);

            if (!Quotes.Any() && TotalRecords > 0)
            {
                _logger.LogWarning("No quotes found for page {Page}, but TotalRecords={TotalRecords}. Possible pagination or filter issue.", CurrentPage, TotalRecords);
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

        // Helper method to detect changes between two Quote objects
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

            // if (oldQuote.DiscountAmount != newQuote.DiscountAmount)
            //     changes.Add(("DiscountAmount", oldQuote.DiscountAmount?.ToString("F2"), newQuote.DiscountAmount?.ToString("F2")));

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

        // Helper method to log history
        private async Task LogHistoryAsync(int quoteId, string action, string? fieldName, string? oldValue, string? newValue, string modifiedBy, string comment)
        {
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
        Teljesístve,
        Lezárva
    }
}