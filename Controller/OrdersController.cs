using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Cloud9_2.Services;
using Cloud9_2.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(OrderService orderService, UserManager<ApplicationUser> userManager, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderDTO>> CreateOrder([FromBody] OrderCreateDTO orderDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for order creation");
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return Unauthorized("User not authenticated");
                }

                var order = await _orderService.CreateOrderAsync(orderDto, userId);
                var orderDtoResponse = MapToOrderDTO(order);
                _logger.LogInformation("Created order with ID {OrderId}", order.OrderId);
                return CreatedAtAction(nameof(GetOrder), new { orderId = order.OrderId }, orderDtoResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        [HttpGet("{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderDTO>> GetOrder(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found", orderId);
                    return NotFound($"Order with ID {orderId} not found");
                }
                return Ok(MapToOrderDTO(order));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<OrderDTO>>> GetAllOrders()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                var orderDtos = orders.Select(MapToOrderDTO).ToList();
                _logger.LogInformation("Retrieved {Count} orders", orderDtos.Count);
                return Ok(orderDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        [HttpPut("{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderDTO>> UpdateOrder(int orderId, [FromBody] OrderUpdateDTO orderDto)
        {
            if (!ModelState.IsValid || orderDto.OrderId != orderId)
            {
                _logger.LogWarning("Invalid model state or ID mismatch for order {OrderId}", orderId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return Unauthorized("User not authenticated");
                }

                var existingOrder = await _orderService.GetOrderByIdAsync(orderId);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found", orderId);
                    return NotFound($"Order with ID {orderId} not found");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (existingOrder.CreatedBy != (user?.UserName ?? "System"))
                {
                    _logger.LogWarning("User {UserId} attempted to update order {OrderId} they do not own", userId, orderId);
                    return Forbid("You do not have permission to update this order");
                }

                var updatedOrder = await _orderService.UpdateOrderAsync(orderDto, userId);
                if (updatedOrder == null)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found during update", orderId);
                    return NotFound($"Order with ID {orderId} not found");
                }

                _logger.LogInformation("Updated order with ID {OrderId}", orderId);
                return Ok(MapToOrderDTO(updatedOrder));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        [HttpDelete("{orderId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteOrder(int orderId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return Unauthorized("User not authenticated");
                }

                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found", orderId);
                    return NotFound($"Order with ID {orderId} not found");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (order.CreatedBy != (user?.UserName ?? "System"))
                {
                    _logger.LogWarning("User {UserId} attempted to delete order {OrderId} they do not own", userId, orderId);
                    return Forbid("You do not have permission to delete this order");
                }

                var result = await _orderService.DeleteOrderAsync(orderId);
                if (!result)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found during deletion", orderId);
                    return NotFound($"Order with ID {orderId} not found");
                }

                _logger.LogInformation("Deleted order with ID {OrderId}", orderId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        private OrderDTO MapToOrderDTO(Order order)
        {
            return new OrderDTO
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Deadline = order.Deadline,
                Description = order.Description,
                TotalAmount = order.TotalAmount,
                SalesPerson = order.SalesPerson,
                DeliveryDate = order.DeliveryDate,
                PlannedDelivery = order.PlannedDelivery,
                DiscountPercentage = order.DiscountPercentage,
                DiscountAmount = order.DiscountAmount,
                CompanyName = order.CompanyName,
                Subject = order.Subject,
                DetailedDescription = order.DetailedDescription,
                CreatedBy = order.CreatedBy,
                CreatedDate = order.CreatedDate,
                ModifiedBy = order.ModifiedBy,
                ModifiedDate = order.ModifiedDate,
                Status = order.Status,
                PartnerId = order.PartnerId,
                SiteId = order.SiteId,
                CurrencyId = order.CurrencyId,
                ShippingMethodId = order.ShippingMethodId,
                PaymentTermId = order.PaymentTermId,
                ContactId = order.ContactId,
                OrderType = order.OrderType,
                ReferenceNumber = order.ReferenceNumber,
                QuoteId = order.QuoteId,
                OrderItems = order.OrderItems?.Select(oi => new OrderItemDTO
                {
                    OrderItemId = oi.OrderItemId,
                    OrderId = oi.OrderId,
                    Description = oi.Description,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    DiscountAmount = oi.DiscountAmount,
                    CreatedBy = oi.CreatedBy,
                    CreatedDate = oi.CreatedDate,
                    ModifiedBy = oi.ModifiedBy,
                    ModifiedDate = oi.ModifiedDate,
                    DiscountType = oi.DiscountType,
                    ProductId = oi.ProductId,
                    VatTypeId = oi.VatTypeId
                }).ToList()
            };
        }
    }
}