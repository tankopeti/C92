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
using System.ComponentModel.DataAnnotations;

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

        // POST: api/Orders
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderDTO>> CreateOrder([FromBody] OrderCreateDTO orderDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Érvénytelen modell állapot a rendelés létrehozásakor: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(new
                {
                    Message = "Érvénytelen adatok.",
                    Errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Felhasználó nem található a jogosultságokban");
                    return Unauthorized(new { Message = "Felhasználó nincs hitelesítve" });
                }

                var order = await _orderService.CreateOrderAsync(orderDto, user.Id);
                var orderDtoResponse = MapToOrderDTO(order);
                _logger.LogInformation("Sikeresen létrehozva a rendelés, ID: {OrderId}", order.OrderId);
                return CreatedAtAction(nameof(GetOrder), new { orderId = order.OrderId }, orderDtoResponse);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Érvénytelen adat a rendelés létrehozásakor");
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Váratlan hiba a rendelés létrehozása során");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Belső szerverhiba" });
            }
        }
        
                // GET: api/Orders/{orderId}
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
                    _logger.LogWarning("Nem található rendelés ezzel az ID-val: {OrderId}", orderId);
                    return NotFound($"Nem található rendelés ezzel az ID-val: {orderId}");
                }
                return Ok(MapToOrderDTO(order));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hiba a rendelés lekérdezése során, ID: {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Belső szerverhiba");
            }
        }

        // GET: api/Orders
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<OrderDTO>>> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync(); // Assumes OrderService supports pagination
                var orderDtos = orders.Select(MapToOrderDTO).ToList();
                // _logger.LogInformation("Sikeresen lekérdezve {Count} rendelés, oldal: {Page}, méret: {PageSize}", orderDtos.Count, page, pageSize);
                return Ok(orderDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hiba a rendelések lekérdezése során");
                return StatusCode(StatusCodes.Status500InternalServerError, "Belső szerverhiba");
            }
        }

        // PUT: api/Orders/{orderId}
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
                _logger.LogWarning("Érvénytelen modell állapot vagy ID eltérés a rendelésnél: {OrderId}", orderId);
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Felhasználó ID nem található a jogosultságokban");
                    return Unauthorized("Felhasználó nincs hitelesítve");
                }

                var existingOrder = await _orderService.GetOrderByIdAsync(orderId);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Nem található rendelés ezzel az ID-val: {OrderId}", orderId);
                    return NotFound($"Nem található rendelés ezzel az ID-val: {orderId}");
                }

                var user = await _userManager.FindByIdAsync(userId);
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin && existingOrder.CreatedBy != (user?.UserName ?? "System"))
                {
                    _logger.LogWarning("Felhasználó {UserId} megpróbálta frissíteni a rendelést {OrderId}, amit nem ő hozott létre", userId, orderId);
                    return Forbid("Nincs jogosultsága a rendelés frissítéséhez");
                }

                var updatedOrder = await _orderService.UpdateOrderAsync(orderDto, userId);
                if (updatedOrder == null)
                {
                    _logger.LogWarning("Nem található rendelés ezzel az ID-val a frissítés során: {OrderId}", orderId);
                    return NotFound($"Nem található rendelés ezzel az ID-val: {orderId}");
                }

                _logger.LogInformation("Sikeresen frissítve a rendelés, ID: {OrderId}", orderId);
                return Ok(MapToOrderDTO(updatedOrder));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Érvénytelen adat a rendelés frissítésekor: {OrderId}", orderId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hiba a rendelés frissítése során: {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Belső szerverhiba");
            }
        }

        // DELETE: api/Orders/{orderId}
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
                    _logger.LogWarning("Felhasználó ID nem található a jogosultságokban");
                    return Unauthorized("Felhasználó nincs hitelesítve");
                }

                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Nem található rendelés ezzel az ID-val: {OrderId}", orderId);
                    return NotFound($"Nem található rendelés ezzel az ID-val: {orderId}");
                }

                var user = await _userManager.FindByIdAsync(userId);
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin && order.CreatedBy != (user?.UserName ?? "System"))
                {
                    _logger.LogWarning("Felhasználó {UserId} megpróbálta törölni a rendelést {OrderId}, amit nem ő hozott létre", userId, orderId);
                    return Forbid("Nincs jogosultsága a rendelés törléséhez");
                }

                var result = await _orderService.DeleteOrderAsync(orderId);
                if (!result)
                {
                    _logger.LogWarning("Nem található rendelés ezzel az ID-val a törlés során: {OrderId}", orderId);
                    return NotFound($"Nem található rendelés ezzel az ID-val: {orderId}");
                }

                _logger.LogInformation("Sikeresen törölve a rendelés, ID: {OrderId}", orderId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hiba a rendelés törlése során: {OrderId}", orderId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Belső szerverhiba");
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
                PartnerName = order.Partner?.Name, // Assumes Partner has a Name property
                SiteId = order.SiteId,
                SiteName = order.Site?.SiteName, // Assumes Site has a Name property
                CurrencyId = order.CurrencyId,
                CurrencyCode = order.Currency?.CurrencyName, // Assumes Currency has a Code property
                ShippingMethodId = order.ShippingMethodId,
                ShippingMethodName = order.ShippingMethod?.MethodName, // Assumes OrderShippingMethod has a Name property
                PaymentTermId = order.PaymentTermId,
                PaymentTermName = order.PaymentTerm?.TermName, // Assumes PaymentTerm has a Name property
                ContactId = order.ContactId,
                ContactName = order.Contact?.FirstName, // Assumes Contact has a Name property
                OrderType = order.OrderType,
                ReferenceNumber = order.ReferenceNumber,
                QuoteId = order.QuoteId,
                IsDeleted = order.IsDeleted,
                OrderStatusTypes = order.OrderStatusTypes,
                // OrderStatusTypeName = order.OrderStatusType?.Name, // Assumes OrderStatusType has a Name property
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
                    ProductName = oi.Product?.Name, // Assumes Product has a Name property
                    VatTypeId = oi.VatTypeId,
                    VatRate = oi.VatType?.Rate, // Assumes VatType has a Rate property
                    LineTotal = oi.LineTotal
                }).ToList()
            };
        }
    }
}