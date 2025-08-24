using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Cloud9_2.Models;
using Cloud9_2.Data;
using Microsoft.Extensions.Logging;
using Cloud9_2.Services;


[Route("api/orders")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrdersController> _logger;
    private readonly IOrderService _orderService;

    public OrdersController(ApplicationDbContext context, ILogger<OrdersController> logger, IOrderService orderService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    [HttpGet("{id}")]
    [Authorize]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        try
        {
            var order = await _orderService.GetOrderAsync(id);

            if (order == null)
            {
                _logger.LogWarning("Order {id} not found", id);
                return NotFound();
            }

            _logger.LogInformation("Fetched order {id} with {ItemCount} items: {OrderItems}", id, order.OrderItems?.Count ?? 0, order.OrderItems?.Select(oi => new { oi.OrderItemId, oi.ProductId }));
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order {id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("partners")]
    [Authorize]
    public async Task<IActionResult> GetPartners(string term = "")
    {
        _logger.LogInformation("Fetching partners with term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
        try
        {
            var partners = await _orderService.GetPartnersAsync();
            var filteredPartners = partners
                .Where(p => string.IsNullOrEmpty(term) || (p.Name != null && p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(p => p.Name)
                .Take(100)
                .Select(p => new
                {
                    id = p.PartnerId,
                    text = !string.IsNullOrWhiteSpace(p.Name) ? p.Name.Trim() : $"Partner {p.PartnerId}"
                })
                .ToList();

            if (!filteredPartners.Any())
            {
                _logger.LogInformation("No partners found for term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
                return Ok(new[] { new { id = (string)null, text = "Nincs találat" } });
            }

            _logger.LogInformation("Fetched {Count} partners", filteredPartners.Count);
            return Ok(filteredPartners);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching partners for term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("sites")]
    [Authorize]
    public async Task<IActionResult> GetSites(int partnerId, string term = "")
    {
        _logger.LogInformation("Fetching sites for partnerId: {PartnerId}, term: {Term}", partnerId, string.IsNullOrEmpty(term) ? "(empty)" : term);
        try
        {
            var query = _context.Sites.Where(s => s.PartnerId == partnerId);
            if (!string.IsNullOrEmpty(term))
            {
                term = term.Trim();
                query = query.Where(s => s.SiteName != null && s.SiteName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var sites = await query
                .OrderBy(s => s.SiteName)
                .Select(s => new
                {
                    id = s.SiteId,
                    text = !string.IsNullOrWhiteSpace(s.SiteName) ? s.SiteName.Trim() : $"Site {s.SiteId}"
                })
                .Take(50)
                .ToListAsync();

            if (!sites.Any())
            {
                _logger.LogInformation("No sites found for partnerId: {PartnerId}, term: {Term}", partnerId, string.IsNullOrEmpty(term) ? "(empty)" : term);
                return Ok(new[] { new { id = (string)null, text = "Nincs találat" } });
            }

            _logger.LogInformation("Fetched {Count} sites", sites.Count);
            return Ok(sites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sites for partnerId: {PartnerId}, term: {Term}", partnerId, string.IsNullOrEmpty(term) ? "(empty)" : term);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("currencies")]
    [Authorize]
    public async Task<IActionResult> GetCurrencies(string term = "")
    {
        _logger.LogInformation("Fetching currencies with term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
        try
        {
            var query = _context.Currencies.AsQueryable();
            if (!string.IsNullOrEmpty(term))
            {
                term = term.Trim();
                query = query.Where(c => c.CurrencyName != null && c.CurrencyName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var currencies = await query
                .OrderBy(c => c.CurrencyName)
                .Select(c => new
                {
                    id = c.CurrencyId,
                    text = !string.IsNullOrWhiteSpace(c.CurrencyName) ? c.CurrencyName.Trim() : $"Currency {c.CurrencyId}"
                })
                .Take(50)
                .ToListAsync();

            if (!currencies.Any())
            {
                _logger.LogInformation("No currencies found for term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
                return Ok(new[] { new { id = (string)null, text = "Nincs találat" } });
            }

            _logger.LogInformation("Fetched {Count} currencies", currencies.Count);
            return Ok(currencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching currencies for term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("quotes")]
    [Authorize]
    public async Task<IActionResult> GetQuotes(string term = "")
    {
        _logger.LogInformation("Fetching quotes with term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
        try
        {
            var query = _context.Quotes.AsQueryable();
            if (!string.IsNullOrEmpty(term))
            {
                term = term.Trim();
                query = query.Where(q => q.QuoteNumber != null && q.QuoteNumber.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var quotes = await query
                .OrderBy(q => q.QuoteNumber)
                .Select(q => new
                {
                    id = q.QuoteId,
                    text = !string.IsNullOrWhiteSpace(q.QuoteNumber) ? q.QuoteNumber.Trim() : $"Quote {q.QuoteId}"
                })
                .Take(50)
                .ToListAsync();

            if (!quotes.Any())
            {
                _logger.LogInformation("No quotes found for term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
                return Ok(new[] { new { id = (string)null, text = "Nincs találat" } });
            }

            _logger.LogInformation("Fetched {Count} quotes", quotes.Count);
            return Ok(quotes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching quotes for term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("products")]
    [Authorize]
    public async Task<IActionResult> GetProducts(string term = "")
    {
        _logger.LogInformation("Fetching products with term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
        try
        {
            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(term))
            {
                term = term.Trim();
                query = query.Where(p => p.Name != null && p.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var products = await query
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    id = p.ProductId,
                    text = !string.IsNullOrWhiteSpace(p.Name) ? p.Name.Trim() : $"Product {p.ProductId}",
                    listPrice = 0 // Adjust based on your Product model if it includes pricing
                })
                .Take(50)
                .ToListAsync();

            if (!products.Any())
            {
                _logger.LogInformation("No products found for term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
                return Ok(new[] { new { id = (string)null, text = "Nincs találat" } });
            }

            _logger.LogInformation("Fetched {Count} products", products.Count);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products for term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
    {
        _logger.LogInformation("Creating new order for partnerId: {PartnerId}", order.PartnerId);
        try
        {
            var createdOrder = await _orderService.CreateOrderAsync(order);
            return Ok(new { orderNumber = createdOrder.OrderNumber, id = createdOrder.OrderId });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for partnerId: {PartnerId}", order.PartnerId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order order)
    {
        _logger.LogInformation("Updating order {OrderId} for partnerId: {PartnerId}", id, order.PartnerId);
        try
        {
            if (id != order.OrderId)
            {
                _logger.LogWarning("OrderId mismatch: URL id {Id} does not match body id {OrderId}", id, order.OrderId);
                return BadRequest(new { error = "OrderId mismatch" });
            }

            var updatedOrder = await _orderService.UpdateOrderAsync(id, order);
            if (updatedOrder == null)
            {
                _logger.LogWarning("Order {OrderId} not found", id);
                return NotFound();
            }

            return Ok(new { orderNumber = updatedOrder.OrderNumber, id = updatedOrder.OrderId });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("{orderId}")]
    [Authorize]
    public async Task<IActionResult> DeleteOrder(int orderId)
    {
        var result = await _orderService.DeleteOrderAsync(orderId);
        if (!result)
        {
            return NotFound(new { error = $"Order {orderId} not found" });
        }
        return Ok();
    }

    [HttpDelete("{orderId}/items/{orderItemId}")]
    [Authorize]
    public async Task<IActionResult> DeleteOrderItem(int orderId, int orderItemId)
    {
        _logger.LogInformation("Deleting order item {OrderItemId} for order {OrderId}", orderItemId, orderId);
        try
        {
            var deleted = await _orderService.DeleteOrderItemAsync(orderId, orderItemId);
            if (!deleted)
            {
                _logger.LogWarning("Order item {OrderItemId} not found for order {OrderId}", orderItemId, orderId);
                return NotFound(new { error = $"Order item {orderItemId} not found for order {orderId}" });
            }
            return Ok(new { message = $"Order item {orderItemId} deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order item {OrderItemId} for order {OrderId}", orderItemId, orderId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("vat/types")]
    [Authorize]
    public async Task<IActionResult> GetVatTypes()
    {
        try
        {
            var vatTypes = await _context.VatTypes
                .Select(v => new
                {
                    vatTypeId = v.VatTypeId,
                    typeName = v.TypeName,
                    rate = v.Rate
                })
                .ToListAsync();
            if (!vatTypes.Any())
            {
                _logger.LogWarning("No VAT types found in database");
                return Ok(new List<object>());
            }
            _logger.LogInformation("Fetched {Count} VAT types", vatTypes.Count);
            return Ok(vatTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching VAT types");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("copy/{orderId}")]
    [Authorize]
    public async Task<ActionResult<Order>> CopyOrder(int orderId)
    {
        try
        {
            if (orderId <= 0)
            {
                _logger.LogWarning("Invalid OrderId provided: {OrderId}", orderId);
                return BadRequest(new { error = "Érvénytelen OrderId: Az azonosítónak pozitív egész számnak kell lennie" });
            }

            _logger.LogInformation("Copy order request for OrderId: {OrderId}", orderId);
            var newOrder = await _orderService.CopyOrderAsync(orderId);
            return Ok(newOrder);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error for OrderId: {OrderId}, Message: {Message}", orderId, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation for OrderId: {OrderId}, Message: {Message}", orderId, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying order for OrderId: {OrderId}", orderId);
            return StatusCode(500, new { error = $"Hiba a megrendelés másolásakor: {ex.Message}" });
        }
    }

    [HttpPost("{id}/send-email")]
    [Authorize] // optional if you want only logged-in users
    public async Task<IActionResult> SendOrderEmail(int id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            // Example: send to partner’s email if available
            var partner = await _context.Partners.FindAsync(order.PartnerId);
            var recipient = partner?.Email ?? "fallback@example.com";

            await _orderService.SendOrderEmailAsync(id, recipient);

            return Ok(new { message = $"Email sent to {recipient}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email for order {OrderId}", id);
            return StatusCode(500, new { message = "Error sending email" });
        }
    }


}