using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires authentication for all actions
    public class QuotesController : ControllerBase
    {
        private readonly QuoteService _quoteService;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuotesController(QuoteService quoteService, UserManager<ApplicationUser> userManager)
        {
            _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
            _userManager = userManager;
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

        // DELETE: api/quotes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuote(int id)
        {
            try
            {
                var result = await _quoteService.DeleteQuoteAsync(id);
                if (!result)
                {
                    return NotFound($"Quote with ID {id} not found.");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the quote: {ex.Message}");
            }
        }
    }
}