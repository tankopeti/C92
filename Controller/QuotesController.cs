using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Controllers
{
public class QuotesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuotesController> _logger;

    public QuotesController(ApplicationDbContext context, ILogger<QuotesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("api/Quotes/nextQuoteNumber")]
    public async Task<IActionResult> GetNextQuoteNumber()
    {
        try
        {
            var lastQuoteNumber = await _context.Quotes
                .Where(q => q.QuoteNumber != null && q.QuoteNumber.StartsWith("QUOTE-"))
                .OrderByDescending(q => q.QuoteNumber)
                .Select(q => q.QuoteNumber)
                .FirstOrDefaultAsync();

            _logger.LogInformation("Last quote number: {LastQuoteNumber}", lastQuoteNumber);

            string nextNumber = "QUOTE-0001";
            if (!string.IsNullOrEmpty(lastQuoteNumber))
            {
                if (int.TryParse(lastQuoteNumber.Replace("QUOTE-", ""), out int lastNumber))
                {
                    nextNumber = $"QUOTE-{lastNumber + 1:D4}";
                }
                else
                {
                    _logger.LogWarning("Invalid quote number format: {LastQuoteNumber}", lastQuoteNumber);
                }
            }

            _logger.LogInformation("Generated next quote number: {NextNumber}", nextNumber);
            return Ok(nextNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating next quote number");
            return StatusCode(500, "Error generating quote number");
        }
    }

    [HttpGet("{quoteId}/items")]
        public async Task<IActionResult> GetQuoteItems(int quoteId)
        {
            var items = await _context.QuoteItems
                .Where(qi => qi.QuoteId == quoteId)
                .Select(qi => new
                {
                    qi.QuoteItemId,
                    qi.QuoteId,
                    qi.ProductId,
                    qi.Quantity,
                    qi.UnitPrice,
                    qi.ItemDescription,
                    qi.DiscountPercentage,
                    qi.DiscountAmount
                })
                .ToListAsync();

            return Ok(items);
        }
}}