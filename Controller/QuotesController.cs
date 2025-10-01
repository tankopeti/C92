using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires authentication for all actions
    public class QuotesController : ControllerBase
    {
        private readonly QuoteService _quoteService;

        public QuotesController(QuoteService quoteService)
        {
            _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
        }

        // POST: api/quotes
        [HttpPost]
        public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteDto createQuoteDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var quote = await _quoteService.CreateQuoteAsync(createQuoteDto);
                return CreatedAtAction(nameof(GetQuote), new { id = quote.QuoteId }, quote);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while creating the quote: {ex.Message}");
            }
        }

        // PUT: api/quotes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuote(int id, [FromBody] UpdateQuoteDto updateQuoteDto)
        {
            if (id != updateQuoteDto.QuoteId)
            {
                return BadRequest("Quote ID in the body must match the ID in the URL.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var quote = await _quoteService.UpdateQuoteAsync(updateQuoteDto);
                return Ok(quote);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the quote: {ex.Message}");
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