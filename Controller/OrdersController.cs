using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Cloud9_2.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ApplicationDbContext context, IOrderService orderService, ILogger<OrdersController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("OrdersController instantiated, Context: {Context}, OrderService: {OrderService}",
                _context != null ? "Not null" : "Null", _orderService != null ? "Not null" : "Null");
        }

        [HttpGet("next-order-number")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNextOrderNumber()
        {
            try
            {
                _logger.LogInformation("Generating next order number.");
                var nextNumber = await _orderService.GetNextOrderNumberAsync();
                _logger.LogInformation("Generated next order number: {NextNumber}", nextNumber);
                return Ok(new { orderNumber = nextNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating next order number.");
                return StatusCode(500, new { error = "Failed to generate order number." });
            }
        }

        [HttpGet("{orderId}/items")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrderItems(int orderId)
        {
            try
            {
                _logger.LogInformation("Fetching items for order ID: {OrderId}", orderId);
                var orderExists = await _context.Orders.AnyAsync(o => o.OrderId == orderId);
                if (!orderExists)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    return NotFound(new { error = $"Order with ID {orderId} not found" });
                }

                var items = await _orderService.GetOrderItemsAsync(orderId);
                _logger.LogInformation("Retrieved {ItemCount} items for order ID: {OrderId}", items.Count(), orderId);
                return Ok(new { items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching items for order ID: {OrderId}", orderId);
                return StatusCode(500, new { error = "Failed to retrieve order items" });
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDto orderDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid order data submitted: {OrderDto}", JsonSerializer.Serialize(orderDto));
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Creating new order for partner ID: {PartnerId}", orderDto.PartnerId);
                var order = await _orderService.CreateOrderAsync(orderDto);
                _logger.LogInformation("Created order with ID: {OrderId}", order.OrderId);
                return CreatedAtAction(nameof(GetOrder), new { orderId = order.OrderId }, order);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating order for PartnerId: {PartnerId}", orderDto.PartnerId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new { error = "Failed to create order" });
            }
        }

        [HttpGet("{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            if (orderId <= 0)
            {
                _logger.LogWarning("Invalid order ID received: {OrderId}", orderId);
                return BadRequest(new { error = "Invalid order ID. It must be a positive integer." });
            }

            try
            {
                _logger.LogInformation("Fetching order ID: {OrderId}", orderId);
                var order = await _orderService.GetOrderByIdAsync(orderId);
                _logger.LogInformation("Retrieved order ID: {OrderId}", order.OrderId);
                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order ID: {OrderId}", orderId);
                return StatusCode(500, new { error = "Failed to retrieve order" });
            }
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string status = null, [FromQuery] int? partnerId = null)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                _logger.LogWarning("Invalid pagination parameters - PageNumber: {PageNumber}, PageSize: {PageSize}", pageNumber, pageSize);
                return BadRequest(new { error = "PageNumber and PageSize must be positive integers." });
            }

            try
            {
                _logger.LogInformation("Fetching orders with pageNumber: {PageNumber}, pageSize: {PageSize}, status: {Status}, partnerId: {PartnerId}",
                    pageNumber, pageSize, status ?? "null", partnerId?.ToString() ?? "null");
                var orders = await _orderService.GetOrdersAsync(pageNumber, pageSize, status, partnerId?.ToString());
                var orderList = orders.ToList();
                if (!orderList.Any())
                {
                    _logger.LogInformation("No orders found for the given criteria.");
                    return Ok(new { orders = new List<OrderDto>(), totalCount = 0 });
                }

                _logger.LogInformation("Retrieved {OrderCount} orders.", orderList.Count);
                return Ok(new { orders = orderList, totalCount = orderList.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders");
                return StatusCode(500, new { error = "Failed to retrieve orders" });
            }
        }

        [HttpGet("partners")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartners()
        {
            try
            {
                _logger.LogInformation("Fetching all partners.");
                var partners = await _orderService.GetPartnersAsync(null);
                if (!partners.Any())
                {
                    _logger.LogWarning("No partners found in the database.");
                    return NotFound(new { message = "No partners found" });
                }

                _logger.LogInformation("Returning {PartnerCount} partners.", partners.Count());
                return Ok(partners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partners.");
                return StatusCode(500, new { error = "Failed to retrieve partners.", details = ex.Message });
            }
        }

        [HttpGet("partners/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartnerById(int id)
        {
            try
            {
                _logger.LogInformation("Fetching partner with ID {PartnerId}.", id);
                var partners = await _orderService.GetPartnersAsync(id.ToString());
                var partner = partners.FirstOrDefault(p => p.PartnerId == id);
                if (partner == null)
                {
                    _logger.LogWarning("Partner with ID {PartnerId} not found.", id);
                    return NotFound(new { message = $"Partner with ID {id} not found" });
                }

                _logger.LogInformation("Returning partner: {PartnerName}", partner.Name);
                return Ok(partner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partner with ID {PartnerId}.", id);
                return StatusCode(500, new { error = "Failed to retrieve partner.", details = ex.Message });
            }
        }

        [HttpPut("{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] OrderDto orderDto)
        {
            _logger.LogInformation("UpdateOrder called for OrderId: {OrderId}, OrderDto: {OrderDto}", orderId, JsonSerializer.Serialize(orderDto));

            if (orderDto == null)
            {
                _logger.LogWarning("UpdateOrder received null OrderDto for OrderId: {OrderId}", orderId);
                return BadRequest(new { error = "Érvénytelen rendelés adatok" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("ModelState validation failed for OrderId: {OrderId}, Errors: {Errors}", orderId, JsonSerializer.Serialize(errors));
                return BadRequest(new { error = "Érvénytelen adatok", details = errors });
            }

            try
            {
                if (orderDto.PartnerId <= 0 || orderDto.CurrencyId <= 0)
                {
                    _logger.LogWarning("Invalid PartnerId or CurrencyId for OrderId: {OrderId}", orderId);
                    return BadRequest(new { error = "PartnerId és CurrencyId kötelező" });
                }

                if (!await _context.Partners.AnyAsync(p => p.PartnerId == orderDto.PartnerId))
                {
                    _logger.LogWarning("Partner not found for PartnerId: {PartnerId}", orderDto.PartnerId);
                    return BadRequest(new { error = $"Érvénytelen PartnerId: {orderDto.PartnerId}" });
                }

                if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == orderDto.CurrencyId))
                {
                    _logger.LogWarning("Currency not found for CurrencyId: {CurrencyId}", orderDto.CurrencyId);
                    return BadRequest(new { error = $"Érvénytelen CurrencyId: {orderDto.CurrencyId}" });
                }

                var result = await _orderService.UpdateOrderAsync(orderId, orderDto);
                _logger.LogInformation("Updated order ID: {OrderId}", orderId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: Order ID {OrderId}", orderId);
                return NotFound(new { error = $"A rendelés nem található: Order ID {orderId}" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating order ID: {OrderId}", orderId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating order ID: {OrderId}", orderId);
                return StatusCode(500, new { error = "Nem sikerült a rendelés frissítése: " + ex.Message });
            }
        }

        [HttpDelete("{orderId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            try
            {
                _logger.LogInformation("Deleting order ID: {OrderId}", orderId);
                var success = await _orderService.DeleteOrderAsync(orderId);
                if (!success)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    return NotFound(new { error = "A rendelés nem található." });
                }
                _logger.LogInformation("Deleted order ID: {OrderId}", orderId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order ID: {OrderId}", orderId);
                return StatusCode(500, new { error = "Hiba történt a rendelés törlése során." });
            }
        }

        [HttpPost("{orderId}/items")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOrderItem(int orderId, [FromBody] OrderItemDto itemDto)
        {
            _logger.LogInformation("CreateOrderItem called for OrderId: {OrderId}, ItemDto: {ItemDto}", orderId, JsonSerializer.Serialize(itemDto));

            if (itemDto == null)
            {
                _logger.LogWarning("CreateOrderItem received null ItemDto for OrderId: {OrderId}", orderId);
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
                _logger.LogWarning("ModelState validation failed for OrderId: {OrderId}, Errors: {Errors}", orderId, JsonSerializer.Serialize(errors));
                return BadRequest(new { error = "Érvénytelen adatok", details = errors });
            }

            try
            {
                if (itemDto.ProductId <= 0)
                    ModelState.AddModelError("ProductId", "ProductId must be a positive number");
                if (itemDto.Quantity <= 0)
                    ModelState.AddModelError("Quantity", "Quantity must be greater than 0");
                if (itemDto.UnitPrice < 0)
                    ModelState.AddModelError("UnitPrice", "UnitPrice cannot be negative");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    _logger.LogWarning("Manual validation failed for OrderId: {OrderId}, Errors: {Errors}", orderId, JsonSerializer.Serialize(errors));
                    return BadRequest(new { error = "Érvénytelen adatok", details = errors });
                }

                var result = await _orderService.CreateOrderItemAsync(orderId, itemDto);
                _logger.LogInformation("Created order item ID: {OrderItemId} for order ID: {OrderId}", result.OrderItemId, orderId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found for OrderId: {OrderId}", orderId);
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error for order ID: {OrderId}", orderId);
                return BadRequest(new { error = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating order item for OrderId: {OrderId}", orderId);
                return BadRequest(new { error = "Adatbázis hiba: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating order item for OrderId: {OrderId}", orderId);
                return StatusCode(500, new { error = "Nem sikerült a tétel létrehozása: " + ex.Message });
            }
        }

        [HttpPut("{orderId}/items/{orderItemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateOrderItem(int orderId, int orderItemId, [FromBody] OrderItemDto itemDto)
        {
            _logger.LogInformation("UpdateOrderItem called for OrderId: {OrderId}, OrderItemId: {OrderItemId}, ItemDto: {ItemDto}",
                orderId, orderItemId, JsonSerializer.Serialize(itemDto));

            if (itemDto == null)
            {
                _logger.LogWarning("UpdateOrderItem received null ItemDto for OrderId: {OrderId}", orderId);
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
                _logger.LogWarning("ModelState validation failed for OrderId: {OrderId}, Errors: {Errors}", orderId, JsonSerializer.Serialize(errors));
                return BadRequest(new { error = "Érvénytelen adatok", details = errors });
            }

            try
            {
                if (itemDto.ProductId <= 0)
                    ModelState.AddModelError("ProductId", "ProductId must be a positive number");
                if (itemDto.Quantity <= 0)
                    ModelState.AddModelError("Quantity", "Quantity must be greater than 0");
                if (itemDto.UnitPrice < 0)
                    ModelState.AddModelError("UnitPrice", "UnitPrice cannot be negative");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    _logger.LogWarning("Manual validation failed for OrderId: {OrderId}, Errors: {Errors}", orderId, JsonSerializer.Serialize(errors));
                    return BadRequest(new { error = "Érvénytelen adatok", details = errors });
                }

                var result = await _orderService.UpdateOrderItemAsync(orderId, orderItemId, itemDto);
                _logger.LogInformation("Updated order item ID: {OrderItemId} for order ID: {OrderId}", orderItemId, orderId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order item not found for OrderId: {OrderId}, OrderItemId: {OrderItemId}", orderId, orderItemId);
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error for order ID: {OrderId}", orderId);
                return BadRequest(new { error = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating order item for OrderId: {OrderId}", orderId);
                return BadRequest(new { error = "Adatbázis hiba: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating order item for OrderId: {OrderId}", orderId);
                return StatusCode(500, new { error = "Nem sikerült a tétel frissítése: " + ex.Message });
            }
        }

        [HttpDelete("{orderId}/items/{orderItemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteOrderItem(int orderId, int orderItemId)
        {
            try
            {
                _logger.LogInformation("Deleting order item ID: {OrderItemId} for order ID: {OrderId}", orderItemId, orderId);
                var success = await _orderService.DeleteOrderItemAsync(orderId, orderItemId);
                if (!success)
                {
                    _logger.LogWarning("Order or item not found: Order ID {OrderId}, Item ID {OrderItemId}", orderId, orderItemId);
                    return NotFound(new { error = "Order or item not found" });
                }
                _logger.LogInformation("Deleted order item ID: {OrderItemId} for order ID: {OrderId}", orderItemId, orderId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order item ID: {OrderItemId} for order ID: {OrderId}", orderItemId, orderId);
                return StatusCode(500, new { error = "Failed to delete order item" });
            }
        }

        [HttpPost("{orderId}/copy")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CopyOrder(int orderId)
        {
            try
            {
                _logger.LogInformation("Copying order ID: {OrderId}", orderId);
                var orderExists = await _context.Orders.AnyAsync(o => o.OrderId == orderId);
                if (!orderExists)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    return NotFound(new { error = $"Order with ID {orderId} not found" });
                }

                var copiedOrder = await _orderService.CopyOrderAsync(orderId);
                _logger.LogInformation("Copied order to new order ID: {NewOrderId}", copiedOrder.OrderId);
                return CreatedAtAction(nameof(GetOrder), new { orderId = copiedOrder.OrderId }, copiedOrder);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying order ID: {OrderId}", orderId);
                return StatusCode(500, new { error = "Failed to copy order" });
            }
        }
    }
}