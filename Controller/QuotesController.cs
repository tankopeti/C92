using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Cloud9_2.Data;
using System.Text.Json;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuotesController : ControllerBase
    {
        private readonly IQuoteService _quoteService;
        private readonly ILogger<QuotesController> _logger;

        private readonly ApplicationDbContext _context;

        public QuotesController(ApplicationDbContext context, IQuoteService quoteService, ILogger<QuotesController> logger)
        {
            _context = context;
            _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("QuotesController instantiated, Context: {Context}, QuoteService: {QuoteService}", 
                _context != null ? "Not null" : "Null", _quoteService != null ? "Not null" : "Null");
        }

        [HttpGet("nextQuoteNumber")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNextQuoteNumber()
        {
            try
            {
                _logger.LogInformation("Generating next quote number.");
                var nextNumber = await _quoteService.GetNextQuoteNumberAsync();
                _logger.LogInformation("Generated next quote number: {NextNumber}", nextNumber);
                return Ok(new { quoteNumber = nextNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating next quote number");
                return StatusCode(500, new { error = "Failed to generate quote number" });
            }
        }

        [HttpGet("{quoteId}/items")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQuoteItems(int quoteId)
        {
            try
            {
                _logger.LogInformation("Fetching items for quote ID: {QuoteId}", quoteId);
                var quoteExists = await _quoteService.QuoteExistsAsync(quoteId);
                if (!quoteExists)
                {
                    _logger.LogWarning("Quote not found: {QuoteId}", quoteId);
                    return NotFound(new { error = $"Quote with ID {quoteId} not found" });
                }

                var items = await _quoteService.GetQuoteItemsAsync(quoteId);
                _logger.LogInformation("Retrieved {ItemCount} items for quote ID: {QuoteId}", items.Count, quoteId);
                return Ok(new { items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching items for quote ID: {QuoteId}", quoteId);
                return StatusCode(500, new { error = "Failed to retrieve quote items" });
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteDto quoteDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid quote data submitted: {@QuoteDto}", quoteDto);
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Creating new quote for partner ID: {PartnerId}", quoteDto.PartnerId);
                var quote = await _quoteService.CreateQuoteAsync(quoteDto);
                _logger.LogInformation("Created quote with ID: {QuoteId}", quote.QuoteId);
                return CreatedAtAction(nameof(GetQuote), new { quoteId = quote.QuoteId }, quote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quote");
                return StatusCode(500, new { error = "Failed to create quote" });
            }
        }

        [HttpGet("{quoteId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQuote(int quoteId)
        {
            if (quoteId <= 0)
            {
                _logger.LogWarning("Invalid quote ID received: {QuoteId}", quoteId);
                return BadRequest(new { error = "Invalid quote ID. It must be a positive integer." });
            }

            try
            {
                _logger.LogInformation("Fetching quote ID: {QuoteId}", quoteId);

                var quote = await _quoteService.GetQuoteByIdAsync(quoteId);
                if (quote == null)
                {
                    _logger.LogWarning("Quote not found: {QuoteId}", quoteId);
                    return NotFound(new { error = $"Quote with ID {quoteId} not found" });
                }

                return Ok(quote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quote ID: {QuoteId}", quoteId);
                return StatusCode(500, new { error = "Failed to retrieve quote" });
            }
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartners()
        {
            try
            {
                _logger.LogInformation("Fetching all partners.");

                var partners = await _quoteService.GetPartnersAsync();
                if (partners == null || !partners.Any())
                {
                    _logger.LogWarning("No partners found in the database.");
                    return NotFound(new { message = "No partners found" });
                }

                _logger.LogInformation($"Returning {partners.Count} partners.");
                return Ok(partners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partners.");
                return StatusCode(500, new { error = "Failed to retrieve partners.", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartnerById(int id)
        {
            try
            {
                _logger.LogInformation($"Fetching partner with ID {id}.");
                var partner = await _context.Partners
                    .Where(p => p.PartnerId == id)
                    .Select(p => new PartnerDto
                    {
                        PartnerId = p.PartnerId,
                        Name = p.Name
                    })
                    .FirstOrDefaultAsync();

                if (partner == null)
                {
                    _logger.LogWarning($"Partner with ID {id} not found.");
                    return NotFound(new { message = $"Partner with ID {id} not found" });
                }

                _logger.LogInformation($"Returning partner: {partner.Name}");
                return Ok(partner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching partner with ID {id}.");
                return StatusCode(500, new { error = "Failed to retrieve partner.", details = ex.Message });
            }
        }

[HttpPut("{quoteId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateQuote(int quoteId, [FromBody] UpdateQuoteDto quoteDto)
    {
        _logger.LogInformation("UpdateQuote called for QuoteId: {QuoteId}, QuoteDto: {QuoteDto}", 
            quoteId, JsonSerializer.Serialize(quoteDto));

        if (quoteDto == null)
        {
            _logger.LogWarning("UpdateQuote received null QuoteDto for QuoteId: {QuoteId}", quoteId);
            return BadRequest(new { error = "Érvénytelen árajánlat adatok" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            _logger.LogWarning("ModelState validation failed for QuoteId: {QuoteId}, Errors: {Errors}", 
                quoteId, JsonSerializer.Serialize(errors));
            return BadRequest(new { error = "Érvénytelen adatok", details = errors });
        }

        try
        {
            if (_context == null)
            {
                _logger.LogError("Database context is null for UpdateQuote QuoteId: {QuoteId}", quoteId);
                return StatusCode(500, new { error = "Adatbázis kapcsolat nem érhető el" });
            }

            if (!await _context.Partners.AnyAsync(p => p.PartnerId == quoteDto.PartnerId))
            {
                _logger.LogWarning("Partner not found for PartnerId: {PartnerId}", quoteDto.PartnerId);
                return BadRequest(new { error = $"Érvénytelen PartnerId: {quoteDto.PartnerId}" });
            }

            var result = await _quoteService.UpdateQuoteAsync(quoteId, quoteDto);
            if (result == null)
            {
                _logger.LogWarning("Quote not found: Quote ID {QuoteId}", quoteId);
                return NotFound(new { error = $"Az árajánlat nem található: Quote ID {quoteId}" });
            }

            _logger.LogInformation("Updated quote ID: {QuoteId}", quoteId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating quote ID: {QuoteId}", quoteId);
            return StatusCode(500, new { error = "Nem sikerült az árajánlat frissítése: " + ex.Message });
        }
    }
    

        [HttpDelete("{quoteId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteQuote(int quoteId)
        {
            try
            {
                _logger.LogInformation("Deleting quote ID: {QuoteId}", quoteId);
                var success = await _quoteService.DeleteQuoteAsync(quoteId);
                if (!success)
                {
                    _logger.LogWarning("Quote not found: {QuoteId}", quoteId);
                    return NotFound(new { error = $"Quote with ID {quoteId} not found" });
                }
                _logger.LogInformation("Deleted quote ID: {QuoteId}", quoteId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quote ID: {QuoteId}", quoteId);
                return StatusCode(500, new { error = "Failed to delete quote" });
            }
        }


[HttpPost("{quoteId}/Items")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> CreateQuoteItem(int quoteId, [FromBody] QuoteItemDto itemDto)
{
    _logger.LogInformation("CreateQuoteItem called for QuoteId: {QuoteId}, ItemDto: {ItemDto}", 
        quoteId, JsonSerializer.Serialize(itemDto));

    if (itemDto == null)
    {
        _logger.LogWarning("CreateQuoteItem received null ItemDto for QuoteId: {QuoteId}", quoteId);
        return BadRequest(new { error = "Érvénytelen tétel adatok" });
    }

    if (!ModelState.IsValid)
    {
        var errors = ModelState
            .Where(x => x.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );
        _logger.LogWarning("ModelState validation failed for QuoteId: {QuoteId}, Errors: {Errors}", 
            quoteId, JsonSerializer.Serialize(errors));
        return BadRequest(new { error = "Érvénytelen adatok", details = errors });
    }

    try
    {
        if (_context == null)
        {
            _logger.LogError("Database context is null for CreateQuoteItem QuoteId: {QuoteId}", quoteId);
            return StatusCode(500, new { error = "Adatbázis kapcsolat nem érhető el" });
        }

        var createDto = new CreateQuoteItemDto
        {
            QuoteId = quoteId,
            ProductId = itemDto.ProductId,
            Quantity = itemDto.Quantity,
            UnitPrice = itemDto.UnitPrice,
            ItemDescription = itemDto.ItemDescription ?? "", // Convert null to empty string
            DiscountPercentage = itemDto.DiscountPercentage,
            DiscountAmount = itemDto.DiscountAmount
        };

        // Validate createDto manually
        if (createDto.ProductId <= 0)
            ModelState.AddModelError("ProductId", "ProductId must be a positive number");
        if (createDto.Quantity <= 0)
            ModelState.AddModelError("Quantity", "Quantity must be greater than 0");
        if (createDto.UnitPrice < 0)
            ModelState.AddModelError("UnitPrice", "UnitPrice cannot be negative");
        if (createDto.QuoteId <= 0)
            ModelState.AddModelError("QuoteId", "QuoteId must be a positive number");

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            _logger.LogWarning("Manual validation failed for QuoteId: {QuoteId}, Errors: {Errors}", 
                quoteId, JsonSerializer.Serialize(errors));
            return BadRequest(new { error = "Érvénytelen adatok", details = errors });
        }

        var result = await _quoteService.CreateQuoteItemAsync(quoteId, createDto);
        if (result == null)
        {
            _logger.LogWarning("Quote not found or invalid data for QuoteId: {QuoteId}", quoteId);
            return NotFound(new { error = $"Az árajánlat nem található vagy érvénytelen adatok: Quote ID {quoteId}" });
        }

        _logger.LogInformation("Created quote item ID: {QuoteItemId} for quote ID: {QuoteId}", result.QuoteItemId, quoteId);
        return Ok(result);
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "Validation error for quote ID: {QuoteId}", quoteId);
        return BadRequest(new { error = ex.Message });
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Database error creating quote item for QuoteId: {QuoteId}", quoteId);
        return BadRequest(new { error = "Adatbázis hiba: " + (ex.InnerException?.Message ?? ex.Message) });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error creating quote item for QuoteId: {QuoteId}", quoteId);
        return StatusCode(500, new { error = "Nem sikerült a tétel létrehozása: " + ex.Message });
    }
}

[HttpPut("{quoteId}/Items/{quoteItemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateQuoteItem(int quoteId, int quoteItemId, [FromBody] UpdateQuoteItemDto itemDto)
    {
        _logger.LogInformation("UpdateQuoteItem called for QuoteId: {QuoteId}, QuoteItemId: {QuoteItemId}, ItemDto: {ItemDto}", 
            quoteId, quoteItemId, JsonSerializer.Serialize(itemDto));

        if (itemDto == null)
        {
            _logger.LogWarning("UpdateQuoteItem received null ItemDto for QuoteId: {QuoteId}", quoteId);
            return BadRequest(new { error = "Érvénytelen tétel adatok" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            _logger.LogWarning("ModelState validation failed for QuoteId: {QuoteId}, Errors: {Errors}", 
                quoteId, JsonSerializer.Serialize(errors));
            return BadRequest(new { error = "Érvénytelen adatok", details = errors });
        }

        try
        {
            if (_context == null)
            {
                _logger.LogError("Database context is null for UpdateQuoteItem QuoteId: {QuoteId}", quoteId);
                return StatusCode(500, new { error = "Adatbázis kapcsolat nem érhető el" });
            }

            var result = await _quoteService.UpdateQuoteItemAsync(quoteId, quoteItemId, itemDto);
            if (result == null)
            {
                _logger.LogWarning("Quote item not found for QuoteId: {QuoteId}, QuoteItemId: {QuoteItemId}", quoteId, quoteItemId);
                return NotFound(new { error = $"A tétel nem található: Quote ID {quoteId}, Item ID {quoteItemId}" });
            }

            _logger.LogInformation("Updated quote item ID: {QuoteItemId} for quote ID: {QuoteId}", quoteItemId, quoteId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error for quote ID: {QuoteId}", quoteId);
            return BadRequest(new { error = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating quote item for QuoteId: {QuoteId}", quoteId);
            return BadRequest(new { error = "Adatbázis hiba: " + (ex.InnerException?.Message ?? ex.Message) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating quote item for QuoteId: {QuoteId}", quoteId);
            return StatusCode(500, new { error = "Nem sikerült a tétel frissítése: " + ex.Message });
        }
    }

        [HttpDelete("{quoteId}/items/{quoteItemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteQuoteItem(int quoteId, int quoteItemId)
        {
            try
            {
                _logger.LogInformation("Deleting quote item ID: {QuoteItemId} for quote ID: {QuoteId}", quoteItemId, quoteId);
                var success = await _quoteService.DeleteQuoteItemAsync(quoteId, quoteItemId);
                if (!success)
                {
                    _logger.LogWarning("Quote or item not found: Quote ID {QuoteId}, Item ID {QuoteItemId}", quoteId, quoteItemId);
                    return NotFound(new { error = $"Quote or item not found" });
                }
                _logger.LogInformation("Deleted quote item ID: {QuoteItemId} for quote ID: {QuoteId}", quoteItemId, quoteId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quote item ID: {QuoteItemId} for quote ID: {QuoteId}", quoteItemId, quoteId);
                return StatusCode(500, new { error = "Failed to delete quote item" });
            }
        }

        [HttpPost("{quoteId}/copy")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CopyQuote(int quoteId)
        {
            try
            {
                _logger.LogInformation("Copying quote ID: {QuoteId}", quoteId);
                var quoteExists = await _quoteService.QuoteExistsAsync(quoteId);
                if (!quoteExists)
                {
                    _logger.LogWarning("Quote not found: {QuoteId}", quoteId);
                    return NotFound(new { error = $"Quote with ID {quoteId} not found" });
                }

                var copiedQuote = await _quoteService.CopyQuoteAsync(quoteId);
                _logger.LogInformation("Copied quote to new quote ID: {NewQuoteId}", copiedQuote.QuoteId);
                return CreatedAtAction(nameof(GetQuote), new { quoteId = copiedQuote.QuoteId }, copiedQuote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying quote ID: {QuoteId}", quoteId);
                return StatusCode(500, new { error = "Failed to copy quote" });
            }
        }
    }
}