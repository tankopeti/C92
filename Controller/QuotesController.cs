using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuotesController : ControllerBase
    {
        private readonly QuoteService _quoteService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuotesController> _logger;

        public QuotesController(
            QuoteService quoteService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<QuotesController> logger)
        {
            _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("select")]
        public async Task<IActionResult> GetQuotesForSelect([FromQuery] int partnerId, [FromQuery] string search = "")
        {
            try
            {
                var quotes = await _context.Quotes
                    .AsNoTracking()
                    .Where(q => q.PartnerId == partnerId &&
                               (string.IsNullOrEmpty(search) || q.QuoteNumber.Contains(search)))
                    .OrderBy(q => q.QuoteNumber)
                    .Select(q => new
                    {
                        id = q.QuoteId,
                        text = q.QuoteNumber
                    })
                    .Take(50)
                    .ToListAsync();

                _logger.LogInformation("Fetched {QuoteCount} quotes for PartnerId: {PartnerId}", quotes.Count, partnerId);
                return Ok(quotes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quotes for PartnerId: {PartnerId}", partnerId);
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "Failed to retrieve quotes" } } });
            }
        }


        // POST: api/quotes
        [HttpPost]
        public async Task<ActionResult<Quote>> CreateQuote([FromBody] CreateQuoteDto createQuoteDto)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not authenticated." });
                }

                var quote = await _quoteService.CreateQuoteAsync(createQuoteDto, user.UserName);
                return CreatedAtAction(nameof(GetQuote), new { id = quote.QuoteId }, quote);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while creating the quote: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuote(int id, [FromBody] UpdateQuoteDto updateQuoteDto)
        {
            if (id != updateQuoteDto.QuoteId)
            {
                return BadRequest(new { message = "Quote ID in the body must match the ID in the URL." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid data provided.", errors = ModelState });
            }

            try
            {
                var quote = await _quoteService.UpdateQuoteAsync(updateQuoteDto);
                return Ok(quote);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while updating the quote: {ex.Message}" });
            }
        }



        // GET: api/quotes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuote(int id)
        {
            try
            {
                var quote = await _quoteService.GetQuoteByIdAsync(id);
                return Ok(quote);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the quote: {ex.Message}");
            }
        }

        // GET: api/quotes
        [HttpGet]
        public async Task<IActionResult> GetAllQuotes()
        {
            try
            {
                var quotes = await _quoteService.GetAllQuotesAsync();
                return Ok(quotes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving quotes: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuote(int id)
        {
            try
            {
                var result = await _quoteService.DeleteQuoteAsync(id);
                if (!result)
                {
                    return NotFound(new { message = $"Quote with ID {id} not found." });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while deleting the quote: {ex.Message}" });
            }
        }

        [HttpPost("{id}/convert-to-order")]
        public async Task<IActionResult> ConvertQuoteToOrder(int id)
        {
            var method = $"{nameof(QuotesController)}.ConvertQuoteToOrder";
            _logger.LogInformation("[{Method}] START – Converting Quote ID: {QuoteId}", method, id);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized(new { message = "User not authenticated." });

                var username = user.UserName ?? user.Email ?? "System";

                var quoteExists = await _context.Quotes
                    .AnyAsync(q => q.QuoteId == id && (q.IsActive == null || q.IsActive == true));

                if (!quoteExists)
                    return NotFound(new { message = $"Quote with ID {id} not found or inactive." });

                var quoteIdParam = new SqlParameter("@QuoteId", id);
                var createdByParam = new SqlParameter("@CreatedBy", SqlDbType.NVarChar, 100) { Value = username };
                var newOrderIdParam = new SqlParameter
                {
                    ParameterName = "@NewOrderId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.usp_ConvertQuoteToOrder @QuoteId, @CreatedBy, @NewOrderId OUTPUT",
                    quoteIdParam, createdByParam, newOrderIdParam);

                var newOrderId = newOrderIdParam.Value as int? ?? 0;

                if (newOrderId <= 0)
                    return StatusCode(500, new { message = "Order created but ID not returned." });

                var order = await _context.Orders
                    .Include(o => o.Partner)
                    .Include(o => o.Currency)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == newOrderId);

                _logger.LogInformation("[{Method}] SUCCESS → Order {OrderId} created from Quote {QuoteId}", method, newOrderId, id);

                return Ok(new
                {
                    message = "Quote successfully converted to order",
                    orderId = newOrderId,
                    orderNumber = order?.OrderNumber,
                    order
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Method}] FAILED converting Quote {QuoteId}", method, id);
                return StatusCode(500, new { message = "A rendelés nem hozható létre (már van ilyen rendelés)", error = ex.Message });
            }
        }


        public class OrderIdResult
        {
            public int NewOrderId { get; set; }
        }


    }
}