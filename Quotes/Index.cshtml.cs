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

        public IList<Quote> Quotes { get; set; }
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

            var query = _context.Quotes
                .Include(q => q.Partner)
                .Include(q => q.QuoteItems).ThenInclude(qi => qi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(q => q.QuoteNumber.Contains(searchTerm) || q.Partner.Name.Contains(searchTerm));
            }

            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);
            Quotes = await query
                .OrderBy(q => q.QuoteId)
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

var lastQuote = await _context.Quotes.OrderByDescending(q => q.QuoteId).FirstOrDefaultAsync();
if (lastQuote != null && !string.IsNullOrEmpty(lastQuote.QuoteNumber))
{
    int nextNumber = int.Parse(lastQuote.QuoteNumber.Substring(1)) + 1;
    NextQuoteNumber = $"Q{nextNumber:D6}";
}
else
{
    NextQuoteNumber = "Q000001";
}
        }

public async Task<IActionResult> OnPostCreateQuoteAsync(Quote quote)
{
    if (!ModelState.IsValid)
    {
        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        _logger.LogError("ModelState invalid: {Errors}", string.Join(", ", errors));
        return BadRequest(new { errors = errors.ToList() });
    }

    // Ensure hu-HU decimal parsing
    quote.DiscountPercentage = quote.DiscountPercentage ?? 0;
    quote.DiscountAmount = quote.DiscountAmount ?? 0;
    quote.TotalAmount = quote.TotalAmount ?? 0;

    if (quote.QuoteId > 0)
    {
        // Update existing quote
        var existingQuote = await _context.Quotes
            .Include(q => q.QuoteItems)
            .FirstOrDefaultAsync(q => q.QuoteId == quote.QuoteId);

        if (existingQuote == null)
        {
            _logger.LogError("Quote not found: QuoteId={QuoteId}", quote.QuoteId);
            return BadRequest(new { error = "Árajánlat nem található." });
        }

        // Update fields
        existingQuote.QuoteNumber = quote.QuoteNumber;
        existingQuote.QuoteDate = quote.QuoteDate;
        existingQuote.PartnerId = quote.PartnerId;
        existingQuote.SalesPerson = quote.SalesPerson;
        existingQuote.ValidityDate = quote.ValidityDate;
        existingQuote.Status = quote.Status ?? "Draft";
        existingQuote.DiscountPercentage = quote.DiscountPercentage;
        existingQuote.DiscountAmount = quote.DiscountAmount;
        existingQuote.ModifiedBy = User.Identity?.Name ?? "System";
        existingQuote.ModifiedDate = DateTime.UtcNow;

        // Recalculate TotalAmount
        decimal itemsTotal = existingQuote.QuoteItems.Sum(qi => qi.Quantity * qi.UnitPrice - (qi.DiscountAmount ?? 0));
        if (existingQuote.DiscountAmount > 0)
        {
            existingQuote.DiscountPercentage = 0;
            existingQuote.TotalAmount = itemsTotal - existingQuote.DiscountAmount;
        }
        else if (existingQuote.DiscountPercentage > 0)
        {
            existingQuote.DiscountAmount = itemsTotal * existingQuote.DiscountPercentage.Value / 100;
            existingQuote.TotalAmount = itemsTotal - existingQuote.DiscountAmount;
        }
        else
        {
            existingQuote.TotalAmount = itemsTotal;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Quote updated: QuoteId={QuoteId}", existingQuote.QuoteId);
        return new JsonResult(new { quoteId = existingQuote.QuoteId });
    }
    else
    {
        // Create new quote
        quote.CreatedBy = quote.CreatedBy ?? User.Identity?.Name ?? "System";
        quote.ModifiedBy = quote.CreatedBy;
        quote.CreatedDate = DateTime.UtcNow;
        quote.ModifiedDate = DateTime.UtcNow;
        quote.Status = quote.Status ?? "Draft";

        // If DiscountAmount is set and non-zero, reset DiscountPercentage
        if (quote.DiscountAmount > 0)
        {
            quote.DiscountPercentage = 0;
        }
        else if (quote.DiscountPercentage > 0 && quote.QuoteItems.Any())
        {
            decimal itemsTotal = quote.QuoteItems.Sum(qi => qi.Quantity * qi.UnitPrice - (qi.DiscountAmount ?? 0));
            quote.DiscountAmount = itemsTotal * quote.DiscountPercentage.Value / 100;
            quote.TotalAmount = itemsTotal - quote.DiscountAmount;
        }

        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Quote created: QuoteId={QuoteId}", quote.QuoteId);
        return new JsonResult(new { quoteId = quote.QuoteId });
    }
}

        public async Task<IActionResult> OnPostAddItemAsync(int quoteId, int productId, int quantity, decimal unitPrice, string itemDescription, decimal? discountPercentage, decimal? discountAmount)
        {
            if (quoteId <= 0 || productId <= 0 || quantity <= 0 || unitPrice < 0)
            {
                _logger.LogError("Invalid item data: QuoteId={QuoteId}, ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}", quoteId, productId, quantity, unitPrice);
                return BadRequest(new { error = "Érvénytelen tétel adatok." });
            }

            var quote = await _context.Quotes.Include(q => q.QuoteItems).FirstOrDefaultAsync(q => q.QuoteId == quoteId);
            if (quote == null)
            {
                _logger.LogError("Quote not found: QuoteId={QuoteId}", quoteId);
                return BadRequest(new { error = "Árajánlat nem található." });
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                _logger.LogError("Product not found: ProductId={ProductId}", productId);
                return BadRequest(new { error = "Termék nem található." });
            }

            var quoteItem = new QuoteItem
            {
                QuoteId = quoteId,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                ItemDescription = itemDescription ?? "",
                DiscountPercentage = discountPercentage ?? 0,
                DiscountAmount = discountAmount ?? (discountPercentage > 0 ? (quantity * unitPrice * (discountPercentage.Value / 100)) : 0)
            };

            quote.QuoteItems.Add(quoteItem);
            quote.ModifiedDate = DateTime.UtcNow;
            quote.ModifiedBy = User.Identity?.Name ?? "System";

            // Calculate total with item-level and quote-level discounts
            decimal itemsTotal = quote.QuoteItems.Sum(qi => qi.Quantity * qi.UnitPrice - (qi.DiscountAmount ?? 0));
            quote.DiscountAmount = (quote.DiscountPercentage > 0) ? (itemsTotal * quote.DiscountPercentage.Value / 100) : (quote.DiscountAmount ?? 0);
            quote.TotalAmount = itemsTotal - quote.DiscountAmount;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Item added to QuoteId: {QuoteId}, QuoteItemId: {QuoteItemId}", quoteId, quoteItem.QuoteItemId);
            return new JsonResult(new { success = true, quoteItemId = quoteItem.QuoteItemId });
        }

public async Task<IActionResult> OnPostUpdateItemAsync(int quoteItemId, int quantity, decimal unitPrice, string itemDescription, decimal? discountPercentage, decimal? discountAmount)
        {
            if (quoteItemId <= 0 || quantity <= 0 || unitPrice < 0)
            {
                _logger.LogError("Invalid update data: QuoteItemId={QuoteItemId}, Quantity={Quantity}, UnitPrice={UnitPrice}", quoteItemId, quantity, unitPrice);
                return BadRequest(new { error = "Érvénytelen tétel adatok." });
            }

            var quoteItem = await _context.QuoteItems
                .Include(qi => qi.Quote)
                .ThenInclude(q => q.QuoteItems)
                .FirstOrDefaultAsync(qi => qi.QuoteItemId == quoteItemId);

            if (quoteItem == null)
            {
                _logger.LogError("QuoteItem not found: QuoteItemId={QuoteItemId}", quoteItemId);
                return BadRequest(new { error = "Tétel nem található." });
            }

            quoteItem.Quantity = quantity;
            quoteItem.UnitPrice = unitPrice;
            quoteItem.ItemDescription = itemDescription ?? "";
            quoteItem.DiscountPercentage = discountPercentage ?? 0;
            quoteItem.DiscountAmount = discountAmount ?? (discountPercentage > 0 ? (quantity * unitPrice * (discountPercentage.Value / 100)) : 0);
            quoteItem.Quote.ModifiedDate = DateTime.UtcNow;
            quoteItem.Quote.ModifiedBy = User.Identity?.Name ?? "System";

            // Update quote totals
            decimal itemsTotal = quoteItem.Quote.QuoteItems.Sum(qi => qi.Quantity * qi.UnitPrice - (qi.DiscountAmount ?? 0));
            quoteItem.Quote.DiscountAmount = (quoteItem.Quote.DiscountPercentage > 0) ? (itemsTotal * quoteItem.Quote.DiscountPercentage.Value / 100) : (quoteItem.Quote.DiscountAmount ?? 0);
            quoteItem.Quote.TotalAmount = itemsTotal - quoteItem.Quote.DiscountAmount;

            await _context.SaveChangesAsync();

            _logger.LogInformation("QuoteItem updated: QuoteItemId={QuoteItemId}", quoteItemId);
            return new JsonResult(new { success = true });
        }

    }
}