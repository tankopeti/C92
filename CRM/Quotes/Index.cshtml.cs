using Cloud9_2.Models;
using Cloud9_2.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.CRM.Quotes
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Quote> Quotes { get; set; }
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public int PageSize { get; set; } = 10;
        public string NextQuoteNumber { get; set; }

        public async Task OnGetAsync(string searchTerm, int? pageNumber, int? pageSize)
        {
            SearchTerm = searchTerm;
            PageSize = pageSize ?? 10;
            CurrentPage = pageNumber ?? 1;

            var query = _context.Quotes
                .Include(q => q.Partner)
                .Include(q => q.QuoteItems)
                .ThenInclude(qi => qi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(q => q.QuoteNumber.Contains(SearchTerm) || q.Status.Contains(SearchTerm));
            }

            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            Quotes = await query
                .OrderBy(q => q.QuoteDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Generate next quote number
            var lastQuote = await _context.Quotes.OrderByDescending(q => q.QuoteId).FirstOrDefaultAsync();
            if (lastQuote != null && !string.IsNullOrEmpty(lastQuote.QuoteNumber))
            {
                string quoteNumber = lastQuote.QuoteNumber;
                string prefix = "Q";
                int number = 1;

                if (quoteNumber.StartsWith("EST-"))
                {
                    prefix = "EST-";
                    string numericPart = quoteNumber.Substring(4);
                    if (int.TryParse(numericPart, out int parsedNumber))
                    {
                        number = parsedNumber + 1;
                    }
                }
                else if (quoteNumber.StartsWith("Q"))
                {
                    prefix = "Q";
                    string numericPart = quoteNumber.Substring(1);
                    if (int.TryParse(numericPart, out int parsedNumber))
                    {
                        number = parsedNumber + 1;
                    }
                }

                NextQuoteNumber = $"{prefix}{number:D3}";
            }
            else
            {
                NextQuoteNumber = "Q000001";
            }
        }

public async Task<IActionResult> OnPostCreateQuoteAsync(Quote quote, List<QuoteItem> QuoteItems)
{
    if (!ModelState.IsValid)
    {
        // Repopulate necessary data for re-rendering
        await PopulateModalDataAsync();
        return Page();
    }

    // Validate QuoteNumber format
    if (!string.IsNullOrEmpty(quote.QuoteNumber) &&
        !quote.QuoteNumber.StartsWith("EST-") && !quote.QuoteNumber.StartsWith("Q"))
    {
        ModelState.AddModelError("Quote.QuoteNumber", "Quote number must start with 'EST-' or 'Q'.");
        await PopulateModalDataAsync();
        return Page();
    }

    quote.CreatedDate = DateTime.UtcNow;
    quote.ModifiedDate = DateTime.UtcNow;
    quote.QuoteItems = QuoteItems ?? new List<QuoteItem>(); // Ensure QuoteItems is not null

    // Handle nullable decimal fields
    quote.DiscountPercentage = quote.DiscountPercentage ?? 0;
    quote.DiscountAmount = quote.DiscountAmount ?? 0;
    quote.TotalAmount = quote.TotalAmount ?? 0;

    _context.Quotes.Add(quote);
    await _context.SaveChangesAsync();
    return RedirectToPage();
}

// Helper method to repopulate modal data
private async Task PopulateModalDataAsync()
{
    Quotes = await _context.Quotes
        .Include(q => q.Partner)
        .Include(q => q.QuoteItems)
        .ThenInclude(qi => qi.Product)
        .ToListAsync();

    var lastQuote = await _context.Quotes.OrderByDescending(q => q.QuoteId).FirstOrDefaultAsync();
    if (lastQuote != null && !string.IsNullOrEmpty(lastQuote.QuoteNumber))
    {
        string prefix = lastQuote.QuoteNumber.StartsWith("EST-") ? "EST-" : "Q";
        string numericPart = lastQuote.QuoteNumber.StartsWith("EST-") ? lastQuote.QuoteNumber.Substring(4) : lastQuote.QuoteNumber.Substring(1);
        if (int.TryParse(numericPart, out int number))
        {
            NextQuoteNumber = $"{prefix}{number + 1:D3}";
        }
        else
        {
            NextQuoteNumber = prefix == "EST-" ? "EST-001" : "Q000001";
        }
    }
    else
    {
        NextQuoteNumber = "Q000001";
    }
}

public async Task<IActionResult> OnPostEditQuoteAsync(Quote quote, List<QuoteItem> QuoteItems)
{
    if (!ModelState.IsValid)
    {
        await PopulateModalDataAsync();
        return Page();
    }

    var existingQuote = await _context.Quotes
        .Include(q => q.QuoteItems)
        .FirstOrDefaultAsync(q => q.QuoteId == quote.QuoteId);

    if (existingQuote == null)
    {
        return NotFound();
    }

    existingQuote.QuoteNumber = quote.QuoteNumber;
    existingQuote.QuoteDate = quote.QuoteDate;
    existingQuote.PartnerId = quote.PartnerId;
    existingQuote.SalesPerson = quote.SalesPerson;
    existingQuote.ValidityDate = quote.ValidityDate;
    existingQuote.Status = quote.Status;
    existingQuote.Subject = quote.Subject;
    existingQuote.Description = quote.Description;
    existingQuote.DetailedDescription = quote.DetailedDescription;
    existingQuote.DiscountPercentage = quote.DiscountPercentage ?? 0;
    existingQuote.DiscountAmount = quote.DiscountAmount ?? 0;
    existingQuote.TotalAmount = quote.TotalAmount ?? 0;
    existingQuote.ModifiedDate = DateTime.UtcNow;

    existingQuote.QuoteItems.Clear();
    existingQuote.QuoteItems.AddRange(QuoteItems ?? new List<QuoteItem>());

    await _context.SaveChangesAsync();
    return RedirectToPage();
}

        public async Task<IActionResult> OnPostDeleteQuoteAsync(int quoteId)
        {
            var quote = await _context.Quotes.FindAsync(quoteId);
            if (quote == null) return NotFound();

            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetCheckRelatedRecordsAsync(int id)
        {
            // Check for related records (e.g., invoices or orders referencing this quote)
            bool hasRelatedRecords = false; // Implement logic as needed
            return new JsonResult(new { hasRelatedRecords });
        }
    }


}