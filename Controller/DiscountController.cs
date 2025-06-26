using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Models;
using Cloud9_2.Services;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DiscountController : ControllerBase
    {
        private readonly IDiscountService _discountService;
        private readonly ILogger<DiscountController> _logger;

        public DiscountController(IDiscountService discountService, ILogger<DiscountController> logger)
        {
            _discountService = discountService ?? throw new ArgumentNullException(nameof(discountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("DiscountController instantiated, DiscountService: {DiscountService}", 
                _discountService != null ? "Not null" : "Null");
        }

        [HttpGet("{entityType}/item/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDiscount(string entityType, int itemId)
        {
            try
            {
                _logger.LogInformation("Fetching discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                var discount = await _discountService.GetDiscountAsync(entityType, itemId);
                if (discount == null)
                {
                    _logger.LogWarning("Discount not found for {EntityType} ItemId: {ItemId}", entityType, itemId);
                    return NotFound(new { error = $"Discount for {entityType} ItemId {itemId} not found" });
                }

                _logger.LogInformation("Retrieved discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return Ok(discount);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return StatusCode(500, new { error = "Failed to retrieve discount" });
            }
        }

        [HttpPost("{entityType}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateDiscount(string entityType, [FromBody] DiscountDto discountDto)
        {
            if (discountDto == null)
            {
                _logger.LogWarning("CreateDiscount received null DiscountDto for {EntityType}", entityType);
                return BadRequest(new { error = "Érvénytelen kedvezmény adatok" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("ModelState validation failed for {EntityType}, Errors: {Errors}", 
                    entityType, JsonSerializer.Serialize(errors));
                return BadRequest(new { error = "Érvénytelen adatok", details = errors });
            }

            try
            {
                var result = await _discountService.CreateDiscountAsync(entityType, discountDto);
                _logger.LogInformation("Created discount for {EntityType} ItemId: {ItemId}", entityType, discountDto.ItemId);
                return CreatedAtAction(nameof(GetDiscount), new { entityType, itemId = discountDto.ItemId }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error for {EntityType} ItemId: {ItemId}", entityType, discountDto.ItemId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating discount for {EntityType} ItemId: {ItemId}", entityType, discountDto.ItemId);
                return StatusCode(500, new { error = "Hiba történt a kedvezmény létrehozása közben: " + ex.Message });
            }
        }

        [HttpPut("{entityType}/item/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateDiscount(string entityType, int itemId, [FromBody] DiscountDto discountDto)
        {
            if (discountDto == null)
            {
                _logger.LogWarning("UpdateDiscount received null DiscountDto for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return BadRequest(new { error = "Érvénytelen kedvezmény adatok" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("ModelState validation failed for {EntityType} ItemId: {ItemId}, Errors: {Errors}", 
                    entityType, itemId, JsonSerializer.Serialize(errors));
                return BadRequest(new { error = "Érvénytelen adatok", details = errors });
            }

            try
            {
                var result = await _discountService.UpdateDiscountAsync(entityType, itemId, discountDto);
                if (result == null)
                {
                    _logger.LogWarning("Discount not found for {EntityType} ItemId: {ItemId}", entityType, itemId);
                    return NotFound(new { error = $"Discount for {entityType} ItemId {itemId} not found" });
                }

                _logger.LogInformation("Updated discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return StatusCode(500, new { error = "Hiba történt a kedvezmény frissítése közben: " + ex.Message });
            }
        }

        [HttpDelete("{entityType}/item/{itemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDiscount(string entityType, int itemId)
        {
            try
            {
                _logger.LogInformation("Deleting discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                var success = await _discountService.DeleteDiscountAsync(entityType, itemId);
                if (!success)
                {
                    _logger.LogWarning("Discount not found for {EntityType} ItemId: {ItemId}", entityType, itemId);
                    return NotFound(new { error = $"Discount for {entityType} ItemId {itemId} not found" });
                }

                _logger.LogInformation("Deleted discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting discount for {EntityType} ItemId: {ItemId}", entityType, itemId);
                return StatusCode(500, new { error = "Hiba történt a kedvezmény törlése közben: " + ex.Message });
            }
        }
    }
}