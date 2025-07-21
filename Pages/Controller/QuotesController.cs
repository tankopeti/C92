using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Cloud9_2.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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
            _context = context ?? throw new ArgumentNullException(nameof(context));
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
            if (quoteDto == null)
            {
                _logger.LogWarning("Received null CreateQuoteDto");
                return BadRequest(new { error = "Invalid quote data: DTO is null" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                _logger.LogWarning("Invalid quote data submitted: {Errors}, QuoteDTO: {@QuoteDTO}", JsonSerializer.Serialize(errors), quoteDto);
                return BadRequest(new { error = "Invalid quote data", details = errors });
            }

            try
            {
                _logger.LogInformation("Creating new quote for partner ID: {PartnerId}, QuoteDTO: {@QuoteDTO}", quoteDto.PartnerId, quoteDto);

                // Validate foreign keys
                if (!await _context.Partners.AnyAsync(p => p.PartnerId == quoteDto.PartnerId))
                {
                    _logger.LogWarning("Invalid PartnerId: {PartnerId}", quoteDto.PartnerId);
                    return BadRequest(new { error = $"Invalid PartnerId: {quoteDto.PartnerId}" });
                }
                if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == quoteDto.CurrencyId))
                {
                    _logger.LogWarning("Invalid CurrencyId: {CurrencyId}", quoteDto.CurrencyId);
                    return BadRequest(new { error = $"Invalid CurrencyId: {quoteDto.CurrencyId}" });
                }

                if (quoteDto.Items == null || !quoteDto.Items.Any())
                {
                    _logger.LogWarning("No items provided for quote creation");
                    return BadRequest(new { error = "A quote must contain at least one item" });
                }

                // Validate quote-level discounts
                if (quoteDto.DiscountPercentage.HasValue && (quoteDto.DiscountPercentage < 0 || quoteDto.DiscountPercentage > 100))
                {
                    _logger.LogWarning("Invalid DiscountPercentage: {DiscountPercentage}", quoteDto.DiscountPercentage);
                    return BadRequest(new { error = "DiscountPercentage must be between 0 and 100" });
                }
                if (quoteDto.DiscountAmount.HasValue && quoteDto.DiscountAmount < 0)
                {
                    _logger.LogWarning("Invalid QuoteDiscountAmount: {QuoteDiscountAmount}", quoteDto.DiscountAmount);
                    return BadRequest(new { error = "QuoteDiscountAmount must be non-negative" });
                }
                if (quoteDto.TotalItemDiscounts.HasValue && quoteDto.TotalItemDiscounts < 0)
                {
                    _logger.LogWarning("Invalid TotalItemDiscounts: {TotalItemDiscounts}", quoteDto.TotalItemDiscounts);
                    return BadRequest(new { error = "TotalItemDiscounts must be non-negative" });
                }

                foreach (var item in quoteDto.Items)
                {
                    if (!await _context.Products.AnyAsync(p => p.ProductId == item.ProductId))
                    {
                        _logger.LogWarning("Invalid ProductId: {ProductId}", item.ProductId);
                        return BadRequest(new { error = $"Invalid product ID: {item.ProductId}" });
                    }
                    if (!await _context.VatTypes.AnyAsync(v => v.VatTypeId == item.VatTypeId))
                    {
                        _logger.LogWarning("Invalid VatTypeId: {VatTypeId}", item.VatTypeId);
                        return BadRequest(new { error = $"Invalid VatTypeId: {item.VatTypeId}" });
                    }
                    if (item.Quantity <= 0)
                    {
                        _logger.LogWarning("Invalid Quantity for ProductId: {ProductId}", item.ProductId);
                        return BadRequest(new { error = $"Quantity must be positive for product ID: {item.ProductId}" });
                    }
                    if (item.NetDiscountedPrice < 0)
                    {
                        _logger.LogWarning("Invalid NetDiscountedPrice for ProductId: {ProductId}", item.ProductId);
                        return BadRequest(new { error = $"NetDiscountedPrice must be non-negative for product ID: {item.ProductId}" });
                    }
                    if (item.DiscountTypeId.HasValue)
                    {
                        if (!Enum.IsDefined(typeof(DiscountType), item.DiscountTypeId.Value))
                        {
                            _logger.LogWarning("Invalid DiscountTypeId: {DiscountTypeId} for ProductId: {ProductId}", item.DiscountTypeId, item.ProductId);
                            return BadRequest(new { error = $"Invalid DiscountTypeId: {item.DiscountTypeId} for product ID: {item.ProductId}" });
                        }
                        if (item.DiscountTypeId == (int)DiscountType.PartnerPrice && !item.PartnerPrice.HasValue)
                        {
                            _logger.LogWarning("Missing PartnerPrice for DiscountTypeId: 3 for ProductId: {ProductId}", item.ProductId);
                            return BadRequest(new { error = $"PartnerPrice is required for DiscountTypeId: 3 for product ID: {item.ProductId}" });
                        }
                        if (item.DiscountTypeId != (int)DiscountType.NoDiscount && item.DiscountAmount < 0)
                        {
                            _logger.LogWarning("Invalid DiscountAmount: {DiscountAmount} for ProductId: {ProductId}", item.DiscountAmount, item.ProductId);
                            return BadRequest(new { error = $"DiscountAmount must be non-negative for product ID: {item.ProductId}" });
                        }
                        if (item.DiscountTypeId == (int)DiscountType.NoDiscount && item.DiscountAmount.HasValue)
                        {
                            _logger.LogWarning("DiscountAmount provided for NoDiscount type for ProductId: {ProductId}", item.ProductId);
                            return BadRequest(new { error = $"DiscountAmount must be null for NoDiscount type for product ID: {item.ProductId}" });
                        }
                    }
                }

                var quote = await _quoteService.CreateQuoteAsync(quoteDto);
                _logger.LogInformation("Created quote with ID: {QuoteId}", quote.QuoteId);
                return CreatedAtAction(nameof(GetQuote), new { quoteId = quote.QuoteId }, quote);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating quote for PartnerId: {PartnerId}", quoteDto.PartnerId);
                return BadRequest(new { error = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating quote for PartnerId: {PartnerId}", quoteDto.PartnerId);
                return StatusCode(500, new { error = "Database error creating quote", errorDetails = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating quote for PartnerId: {PartnerId}, QuoteDTO: {@QuoteDTO}", quoteDto.PartnerId, quoteDto);
                return StatusCode(500, new { error = "Failed to create quote", errorDetails = ex.Message, stackTrace = ex.StackTrace });
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

        [HttpGet("partner/{partnerId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartnerById(int partnerId)
        {
            try
            {
                _logger.LogInformation($"Fetching partner with ID {partnerId}.");
                var partner = await _context.Partners
                    .Where(p => p.PartnerId == partnerId)
                    .Select(p => new Cloud9_2.Models.PartnerDto
                    {
                        PartnerId = p.PartnerId,
                        Name = p.Name,
                        Email = p.Email,
                        PhoneNumber = p.PhoneNumber,
                        AlternatePhone = p.AlternatePhone,
                        Website = p.Website,
                        CompanyName = p.CompanyName,
                        TaxId = p.TaxId,
                        IntTaxId = p.IntTaxId,
                        Industry = p.Industry,
                        AddressLine1 = p.AddressLine1,
                        AddressLine2 = p.AddressLine2,
                        City = p.City,
                        State = p.State,
                        PostalCode = p.PostalCode,
                        Country = p.Country,
                        Status = p.Status,
                        LastContacted = p.LastContacted,
                        Notes = p.Notes,
                        AssignedTo = p.AssignedTo,
                        BillingContactName = p.BillingContactName,
                        BillingEmail = p.BillingEmail,
                        PaymentTerms = p.PaymentTerms,
                        CreditLimit = p.CreditLimit,
                        PreferredCurrency = p.PreferredCurrency,
                        IsTaxExempt = p.IsTaxExempt,
                        PartnerGroupId = p.PartnerGroupId
                    })
                    .FirstOrDefaultAsync();

                if (partner == null)
                {
                    _logger.LogWarning($"Partner with ID {partnerId} not found.");
                    return NotFound(new { message = $"Partner with ID {partnerId} not found" });
                }

                _logger.LogInformation($"Returning partner: {partner.Name}");
                return Ok(partner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching partner with ID {partnerId}.");
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
                if (!await _context.Partners.AnyAsync(p => p.PartnerId == quoteDto.PartnerId))
                {
                    _logger.LogWarning("Partner not found for PartnerId: {PartnerId}", quoteDto.PartnerId);
                    return BadRequest(new { error = $"Érvénytelen PartnerId: {quoteDto.PartnerId}" });
                }
                if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == quoteDto.CurrencyId))
                {
                    _logger.LogWarning("Invalid CurrencyId: {CurrencyId}", quoteDto.CurrencyId);
                    return BadRequest(new { error = $"Érvénytelen CurrencyId: {quoteDto.CurrencyId}" });
                }
                if (quoteDto.DiscountPercentage.HasValue && (quoteDto.DiscountPercentage < 0 || quoteDto.DiscountPercentage > 100))
                {
                    _logger.LogWarning("Invalid DiscountPercentage: {DiscountPercentage}", quoteDto.DiscountPercentage);
                    return BadRequest(new { error = "DiscountPercentage must be between 0 and 100" });
                }
                if (quoteDto.DiscountAmount.HasValue && quoteDto.DiscountAmount < 0)
                {
                    _logger.LogWarning("Invalid QuoteDiscountAmount: {QuoteDiscountAmount}", quoteDto.DiscountAmount);
                    return BadRequest(new { error = "QuoteDiscountAmount must be non-negative" });
                }
                if (quoteDto.TotalItemDiscounts.HasValue && quoteDto.TotalItemDiscounts < 0)
                {
                    _logger.LogWarning("Invalid TotalItemDiscounts: {TotalItemDiscounts}", quoteDto.TotalItemDiscounts);
                    return BadRequest(new { error = "TotalItemDiscounts must be non-negative" });
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
                    return NotFound(new { error = "Az árajánlat nem található." });
                }
                _logger.LogInformation("Deleted quote ID: {QuoteId}", quoteId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quote ID: {QuoteId}", quoteId);
                return StatusCode(500, new { error = "Hiba történt az árajánlat törlése során." });
            }
        }

        [HttpPost("{quoteId}/Items")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateQuoteItem(int quoteId, [FromBody] CreateQuoteItemDto itemDto)
        {
            _logger.LogInformation("CreateQuoteItem called for QuoteId: {QuoteId}, ProductId: {ProductId}, Quantity: {Quantity}", 
                quoteId, itemDto.ProductId, itemDto.Quantity);

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
                var quoteExists = await _quoteService.QuoteExistsAsync(quoteId);
                if (!quoteExists)
                {
                    _logger.LogWarning("Quote not found for QuoteId: {QuoteId}", quoteId);
                    return NotFound(new { error = "Az árajánlat nem található" });
                }

                if (!await _context.Products.AnyAsync(p => p.ProductId == itemDto.ProductId))
                {
                    _logger.LogWarning("Invalid ProductId: {ProductId}", itemDto.ProductId);
                    return BadRequest(new { error = $"Invalid product ID: {itemDto.ProductId}" });
                }
                if (!await _context.VatTypes.AnyAsync(v => v.VatTypeId == itemDto.VatTypeId))
                {
                    _logger.LogWarning("Invalid VatTypeId: {VatTypeId}", itemDto.VatTypeId);
                    return BadRequest(new { error = $"Invalid VatTypeId: {itemDto.VatTypeId}" });
                }
                if (itemDto.Quantity <= 0)
                {
                    _logger.LogWarning("Invalid Quantity for ProductId: {ProductId}", itemDto.ProductId);
                    return BadRequest(new { error = $"Quantity must be positive for product ID: {itemDto.ProductId}" });
                }
                if (itemDto.NetDiscountedPrice < 0)
                {
                    _logger.LogWarning("Invalid NetDiscountedPrice for ProductId: {ProductId}", itemDto.ProductId);
                    return BadRequest(new { error = $"NetDiscountedPrice must be non-negative for product ID: {itemDto.ProductId}" });
                }
                if (itemDto.DiscountTypeId.HasValue)
                {
                    if (!Enum.IsDefined(typeof(DiscountType), itemDto.DiscountTypeId.Value))
                    {
                        _logger.LogWarning("Invalid DiscountTypeId: {DiscountTypeId} for ProductId: {ProductId}", itemDto.DiscountTypeId, itemDto.ProductId);
                        return BadRequest(new { error = $"Invalid DiscountTypeId: {itemDto.DiscountTypeId} for product ID: {itemDto.ProductId}" });
                    }
                    if (itemDto.DiscountTypeId != (int)DiscountType.NoDiscount && itemDto.DiscountAmount < 0)
                    {
                        _logger.LogWarning("Invalid DiscountAmount: {DiscountAmount} for ProductId: {ProductId}", itemDto.DiscountAmount, itemDto.ProductId);
                        return BadRequest(new { error = $"DiscountAmount must be non-negative for product ID: {itemDto.ProductId}" });
                    }
                    if (itemDto.DiscountTypeId == (int)DiscountType.NoDiscount && itemDto.DiscountAmount.HasValue)
                    {
                        _logger.LogWarning("DiscountAmount provided for NoDiscount type for ProductId: {ProductId}", itemDto.ProductId);
                        return BadRequest(new { error = $"DiscountAmount must be null for NoDiscount type for product ID: {itemDto.ProductId}" });
                    }
                }

                var result = await _quoteService.CreateQuoteItemAsync(quoteId, itemDto);
                _logger.LogInformation("Created QuoteItem with ID: {QuoteItemId} for QuoteId: {QuoteId}", 
                    result.QuoteItemId, quoteId);
                return CreatedAtAction(nameof(GetQuoteItems), new { quoteId = quoteId }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error for QuoteId: {QuoteId}", quoteId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating QuoteItem for QuoteId: {QuoteId}", quoteId);
                return StatusCode(500, new { error = "Hiba történt a tétel létrehozása közben: " + ex.Message });
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
                if (!await _context.Products.AnyAsync(p => p.ProductId == itemDto.ProductId))
                {
                    _logger.LogWarning("Invalid ProductId: {ProductId}", itemDto.ProductId);
                    return BadRequest(new { error = $"Invalid product ID: {itemDto.ProductId}" });
                }
                if (!await _context.VatTypes.AnyAsync(v => v.VatTypeId == itemDto.VatTypeId))
                {
                    _logger.LogWarning("Invalid VatTypeId: {VatTypeId}", itemDto.VatTypeId);
                    return BadRequest(new { error = $"Invalid VatTypeId: {itemDto.VatTypeId}" });
                }
                if (itemDto.Quantity <= 0)
                {
                    _logger.LogWarning("Invalid Quantity for ProductId: {ProductId}", itemDto.ProductId);
                    return BadRequest(new { error = $"Quantity must be positive for product ID: {itemDto.ProductId}" });
                }
                if (itemDto.NetDiscountedPrice < 0)
                {
                    _logger.LogWarning("Invalid NetDiscountedPrice for ProductId: {ProductId}", itemDto.ProductId);
                    return BadRequest(new { error = $"NetDiscountedPrice must be non-negative for product ID: {itemDto.ProductId}" });
                }
                if (itemDto.DiscountTypeId.HasValue)
                {
                    if (!Enum.IsDefined(typeof(DiscountType), itemDto.DiscountTypeId.Value))
                    {
                        _logger.LogWarning("Invalid DiscountTypeId: {DiscountTypeId} for ProductId: {ProductId}", itemDto.DiscountTypeId, itemDto.ProductId);
                        return BadRequest(new { error = $"Invalid DiscountTypeId: {itemDto.DiscountTypeId} for product ID: {itemDto.ProductId}" });
                    }
                    if (itemDto.DiscountTypeId != (int)DiscountType.NoDiscount && itemDto.DiscountAmount < 0)
                    {
                        _logger.LogWarning("Invalid DiscountAmount: {DiscountAmount} for ProductId: {ProductId}", itemDto.DiscountAmount, itemDto.ProductId);
                        return BadRequest(new { error = $"DiscountAmount must be non-negative for product ID: {itemDto.ProductId}" });
                    }
                    if (itemDto.DiscountTypeId == (int)DiscountType.NoDiscount && itemDto.DiscountAmount.HasValue)
                    {
                        _logger.LogWarning("DiscountAmount provided for NoDiscount type for ProductId: {ProductId}", itemDto.ProductId);
                        return BadRequest(new { error = $"DiscountAmount must be null for NoDiscount type for product ID: {itemDto.ProductId}" });
                    }
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
                var quoteExists = await _context.Quotes.AnyAsync(q => q.QuoteId == quoteId);
                if (!quoteExists)
                {
                    _logger.LogWarning("Quote not found: {QuoteId}", quoteId);
                    return NotFound(new { error = $"Quote with ID {quoteId} not found" });
                }

                var copiedQuote = await _quoteService.CopyQuoteAsync(quoteId);
                _logger.LogInformation("Copied quote to new quote ID: {NewQuoteId}", copiedQuote.QuoteId);
                return CreatedAtAction(nameof(GetQuote), new { quoteId = copiedQuote.QuoteId }, new { quoteNumber = copiedQuote.QuoteNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying quote ID: {QuoteId}. StackTrace: {StackTrace}", quoteId, ex.StackTrace);
                return StatusCode(500, new { error = $"Failed to copy quote: {ex.Message}" });
            }
        }

        [HttpPost("{quoteId}/convert-to-order")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConvertQuoteToOrder(int quoteId, [FromBody] ConvertQuoteToOrderDto convertDto)
        {
            _logger.LogInformation("Converting quote ID: {QuoteId} to order", quoteId);

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
                if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == convertDto.CurrencyId))
                {
                    _logger.LogWarning("Invalid CurrencyId: {CurrencyId}", convertDto.CurrencyId);
                    return BadRequest(new { error = $"Invalid CurrencyId: {convertDto.CurrencyId}" });
                }
                if (convertDto.SiteId.HasValue && !await _context.Sites.AnyAsync(s => s.SiteId == convertDto.SiteId.Value))
                {
                    _logger.LogWarning("Invalid SiteId: {SiteId}", convertDto.SiteId);
                    return BadRequest(new { error = $"Invalid SiteId: {convertDto.SiteId}" });
                }

                var userId = User.Identity?.Name ?? "System";
                var order = await _quoteService.ConvertQuoteToOrderAsync(quoteId, convertDto, userId);
                _logger.LogInformation("Converted quote ID: {QuoteId} to order ID: {OrderId}", quoteId, order.OrderId);
                return CreatedAtAction("GetOrder", "Orders", new { orderId = order.OrderId }, order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Quote not found: {QuoteId}", quoteId);
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid input for quote ID: {QuoteId}, Error: {Error}", quoteId, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation for quote ID: {QuoteId}, Error: {Error}", quoteId, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting quote ID: {QuoteId}", quoteId);
                return StatusCode(500, new { error = "Nem sikerült az árajánlat rendeléssé konvertálása: " + ex.Message });
            }
        }
    }
}