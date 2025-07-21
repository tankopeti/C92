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
    public class ordersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<ordersController> _logger;

        private readonly ApplicationDbContext _context;

        public ordersController(ApplicationDbContext context, IOrderService orderService, ILogger<ordersController> logger)
        {
            _context = context;
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("ordersController instantiated, Context: {Context}, orderService: {orderService}", 
                _context != null ? "Not null" : "Null", _orderService != null ? "Not null" : "Null");
        }

    [HttpGet("next-number")]
        public async Task<IActionResult> GetNextOrderNumber()
        {
            try
            {
                if (_orderService == null)
                {
                    _logger.LogError("OrderService is null");
                    return StatusCode(500, new { error = "Service configuration error" });
                }

                var orderNumber = await _orderService.GetNextOrderNumberAsync();
                if (string.IsNullOrEmpty(orderNumber))
                {
                    _logger.LogError("GetNextOrderNumberAsync returned null or empty");
                    return StatusCode(500, new { error = "Invalid order number generated" });
                }

                _logger.LogInformation("Generated order number: {OrderNumber}", orderNumber);
                return Ok(new { orderNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get next order number: {Message}", ex.Message);
                return StatusCode(500, new { error = $"Failed to get order number: {ex.Message}" });
            }
        }

        [HttpGet("{orderId}/items")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetorderItems(int orderId)
        {
            try
            {
                _logger.LogInformation("Fetching items for order ID: {orderId}", orderId);
                var orderExists = await _orderService.OrderExistsAsync(orderId);
                if (!orderExists)
                {
                    _logger.LogWarning("order not found: {orderId}", orderId);
                    return NotFound(new { error = $"order with ID {orderId} not found" });
                }

                var items = await _orderService.GetOrderItemsAsync(orderId);
                _logger.LogInformation("Retrieved {ItemCount} items for order ID: {orderId}", items.Count, orderId);
                return Ok(new { items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching items for order ID: {orderId}", orderId);
                return StatusCode(500, new { error = "Failed to retrieve order items" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto orderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for CreateOrder: {Errors}", 
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ModelState);
                }

                var order = await _orderService.CreateOrderAsync(orderDto);
                _logger.LogInformation("Order created successfully: {OrderNumber}", order.OrderNumber);
                return Ok(order);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Validation error in CreateOrder: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error in CreateOrder: {Message}", ex.InnerException?.Message ?? ex.Message);
                return StatusCode(500, new { error = $"Database error: {ex.InnerException?.Message ?? ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateOrder: {Message}", ex.Message);
                return StatusCode(500, new { error = $"Failed to create order: {ex.Message}" });
            }
        }

        [HttpGet("{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            try
            {
                _logger.LogInformation("Fetching order with ID: {OrderId}", orderId);
                var order = await _orderService.GetOrderAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order not found with ID: {OrderId}", orderId);
                    return NotFound(new { error = $"Order with ID {orderId} not found" });
                }
                _logger.LogInformation("Retrieved order with ID: {OrderId}", orderId);
                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order with ID: {OrderId}", orderId);
                return StatusCode(500, new { error = "Failed to retrieve order" });
            }
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrders(
            [FromQuery] string searchTerm = "",
            [FromQuery] string statusFilter = "",
            [FromQuery] string sortBy = "",
            [FromQuery] int skip = 0,
            [FromQuery] int take = 10)
        {
            try
            {
                _logger.LogInformation("Fetching orders with searchTerm: {SearchTerm}, statusFilter: {StatusFilter}, sortBy: {SortBy}, skip: {Skip}, take: {Take}",
                    searchTerm, statusFilter, sortBy, skip, take);

                if (skip < 0 || take <= 0)
                {
                    _logger.LogWarning("Invalid pagination parameters: skip={Skip}, take={Take}", skip, take);
                    return BadRequest(new { error = "Invalid pagination parameters" });
                }

                var orders = await _orderService.GetOrdersAsync(searchTerm, statusFilter, sortBy, skip, take);
                _logger.LogInformation("Retrieved {OrderCount} orders", orders.Count);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders");
                return StatusCode(500, new { error = "Failed to retrieve orders" });
            }
        }

    [HttpGet("api/partners")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPartners(string term)
    {
    try
    {
        _logger.LogInformation("Fetching partners with term: {term}", term);

        var query = _context.Partners.AsQueryable();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var partners = await query
            .Select(p => new { id = p.PartnerId, text = p.Name }) // Map to { id, text }
            .Take(100)
            .ToListAsync();

        if (partners == null || !partners.Any())
        {
            _logger.LogWarning("No partners found for term: {term}", term);
            return NotFound(new { message = "No partners found" });
        }

        _logger.LogInformation("Returning {count} partners.", partners.Count);
        return Ok(partners);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching partners.");
        return StatusCode(500, new { error = "Failed to retrieve partners.", details = ex.Message });
    }
}

[HttpGet("api/partners/{id}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> GetPartner(int id)
{
    try
    {
        var partner = await _context.Partners
            .Where(p => p.PartnerId == id)
            .Select(p => new { id = p.PartnerId, text = p.Name }) // Map to { id, text }
            .FirstOrDefaultAsync();

        if (partner == null)
        {
            _logger.LogWarning("Partner with ID {id} not found.", id);
            return NotFound(new { message = $"Partner with ID {id} not found" });
        }

        _logger.LogInformation("Returning partner with ID {id}.", id);
        return Ok(partner);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching partner with ID {id}.", id);
        return StatusCode(500, new { error = "Failed to retrieve partner.", details = ex.Message });
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

        [HttpPut("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto updateOrderDto)
        {
            try
            {
                if (updateOrderDto == null)
                {
                    _logger.LogWarning("UpdateOrder received null UpdateOrderDto for OrderId: {OrderId}", id);
                    return BadRequest(new { error = "Érvénytelen rendelés adatok." });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.SelectMany(x => x.Value.Errors).Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("Model validation failed for OrderId: {OrderId}. Errors: {Errors}", id, string.Join("; ", errors));
                    return BadRequest(new { errors });
                }

                var modifiedBy = User.Identity?.Name ?? "System";
                var orderDto = await _orderService.UpdateOrderAsync(id, updateOrderDto, modifiedBy);
                return Ok(orderDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating order {OrderId}: {Message}", id, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found error updating order {OrderId}: {Message}", id, ex.Message);
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error updating order {OrderId}: {Message}", id, ex.Message);
                return StatusCode(500, new { error = "Adatbázis hiba: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating order {OrderId}: {Message}", id, ex.Message);
                return StatusCode(500, new { error = "Váratlan hiba történt a rendelés frissítése során: " + ex.Message });
            }
        }

    [HttpPut("{orderId}/Items/{orderItemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateorderItem(int orderId, int orderItemId, [FromBody] UpdateOrderItemDto itemDto)
    {
        _logger.LogInformation("UpdateorderItem called for orderId: {orderId}, orderItemId: {orderItemId}, ItemDto: {ItemDto}", 
            orderId, orderItemId, JsonSerializer.Serialize(itemDto));

        if (itemDto == null)
        {
            _logger.LogWarning("UpdateorderItem received null ItemDto for orderId: {orderId}", orderId);
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
            _logger.LogWarning("ModelState validation failed for orderId: {orderId}, Errors: {Errors}", 
                orderId, JsonSerializer.Serialize(errors));
            return BadRequest(new { error = "Érvénytelen adatok", details = errors });
        }

        try
        {
            if (_context == null)
            {
                _logger.LogError("Database context is null for UpdateorderItem orderId: {orderId}", orderId);
                return StatusCode(500, new { error = "Adatbázis kapcsolat nem érhető el" });
            }

            var result = await _orderService.UpdateOrderItemAsync(orderId, orderItemId, itemDto);
            if (result == null)
            {
                _logger.LogWarning("order item not found for orderId: {orderId}, orderItemId: {orderItemId}", orderId, orderItemId);
                return NotFound(new { error = $"A tétel nem található: order ID {orderId}, Item ID {orderItemId}" });
            }

            _logger.LogInformation("Updated order item ID: {orderItemId} for order ID: {orderId}", orderItemId, orderId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error for order ID: {orderId}", orderId);
            return BadRequest(new { error = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating order item for orderId: {orderId}", orderId);
            return BadRequest(new { error = "Adatbázis hiba: " + (ex.InnerException?.Message ?? ex.Message) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating order item for orderId: {orderId}", orderId);
            return StatusCode(500, new { error = "Nem sikerült a tétel frissítése: " + ex.Message });
        }
    }

        [HttpDelete("{orderId}/items/{orderItemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteorderItem(int orderId, int orderItemId)
        {
            try
            {
                _logger.LogInformation("Deleting order item ID: {orderItemId} for order ID: {orderId}", orderItemId, orderId);
                var success = await _orderService.DeleteOrderItemAsync(orderId, orderItemId);
                if (!success)
                {
                    _logger.LogWarning("order or item not found: order ID {orderId}, Item ID {orderItemId}", orderId, orderItemId);
                    return NotFound(new { error = $"order or item not found" });
                }
                _logger.LogInformation("Deleted order item ID: {orderItemId} for order ID: {orderId}", orderItemId, orderId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order item ID: {orderItemId} for order ID: {orderId}", orderItemId, orderId);
                return StatusCode(500, new { error = "Failed to delete order item" });
            }
        }

        [HttpPost("{id}/copy")]
        public async Task<IActionResult> CopyOrder(int id)
        {
            try
            {
                var order = await _orderService.CopyOrderAsync(id);
                _logger.LogInformation("Order copied successfully: {OrderNumber}", order.OrderNumber);
                return Ok(order);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Validation error copying order: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error copying order: {Message}", ex.Message);
                return StatusCode(500, new { error = $"Failed to copy order: {ex.Message}" });
            }
        }
    }
}