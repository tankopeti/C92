using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Cloud9_2.Models;
using Cloud9_2.Data;
using System.Text.Json;

[Route("api/orders")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ApplicationDbContext context, ILogger<OrdersController> logger)
    {
        _context = context;
        _logger = logger;
    }

[HttpGet("partners")]
    [Authorize]
    public async Task<IActionResult> GetPartners(string term = "")
    {
        _logger.LogInformation("Fetching partners with term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
        try
        {
            var query = _context.Partners.AsQueryable();
            if (!string.IsNullOrEmpty(term))
            {
                term = term.Trim();
                query = query.Where(p => p.Name != null && p.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var partners = await query
                .OrderBy(p => p.Name)
                .Select(p => new 
                { 
                    id = p.PartnerId, 
                    text = !string.IsNullOrWhiteSpace(p.Name) ? p.Name.Trim() : $"Partner {p.PartnerId}" 
                })
                .Take(100)
                .ToListAsync();

            if (!partners.Any())
            {
                _logger.LogInformation("No partners found for term: {Term}", string.IsNullOrEmpty(term) ? "(empty)" : term);
                return Ok(new[] { new { id = (string)null, text = "Nincs találat" } });
            }

            _logger.LogInformation("Fetched {Count} partners: {Partners}", partners.Count, JsonSerializer.Serialize(partners));
            return Ok(partners);
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

            _logger.LogInformation("Fetched {Count} sites: {Sites}", sites.Count, JsonSerializer.Serialize(sites));
            return Ok(sites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sites for partnerId: {PartnerId}, term: {Term}", partnerId, string.IsNullOrEmpty(term) ? "(empty)" : term);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Creating new order for partnerId: {PartnerId}", request.PartnerId);
        try
        {
            var partner = await _context.Partners.FindAsync(request.PartnerId);
            if (partner == null)
            {
                _logger.LogWarning("Partner not found: {PartnerId}", request.PartnerId);
                return NotFound(new { error = "Partner not found" });
            }

            var order = new Order
            {
                PartnerId = request.PartnerId,
                OrderNumber = request.OrderNumber ?? GenerateOrderNumber(),
                OrderDate = request.OrderDate != null ? DateTime.Parse(request.OrderDate) : DateTime.UtcNow,
                Deadline = request.Deadline != null ? DateTime.Parse(request.Deadline) : null,
                DeliveryDate = request.DeliveryDate != null ? DateTime.Parse(request.DeliveryDate) : null,
                ReferenceNumber = request.ReferenceNumber,
                OrderType = request.OrderType,
                CompanyName = request.CompanyName,
                TotalAmount = request.TotalAmount,
                DiscountPercentage = request.DiscountPercentage,
                DiscountAmount = request.DiscountAmount,
                PaymentTerms = request.PaymentTerms,
                ShippingMethod = request.ShippingMethod,
                SalesPerson = request.SalesPerson,
                Status = request.Status ?? "Pending",
                Subject = request.Subject,
                Description = request.Description,
                DetailedDescription = request.DetailedDescription,
                CreatedBy = request.CreatedBy,
                CreatedDate = request.CreatedDate != null ? DateTime.Parse(request.CreatedDate) : DateTime.UtcNow,
                ModifiedBy = request.ModifiedBy,
                ModifiedDate = request.ModifiedDate != null ? DateTime.Parse(request.ModifiedDate) : DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { orderNumber = order.OrderNumber, id = order.OrderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for partnerId: {PartnerId}", request.PartnerId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private string GenerateOrderNumber()
    {
        return "ORD-" + DateTime.UtcNow.Ticks;
    }
}

public class CreateOrderRequest
{
    public int PartnerId { get; set; }
    public string OrderNumber { get; set; }
    public string OrderDate { get; set; }
    public string Deadline { get; set; }
    public string DeliveryDate { get; set; }
    public string ReferenceNumber { get; set; }
    public string OrderType { get; set; }
    public string CompanyName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string PaymentTerms { get; set; }
    public string ShippingMethod { get; set; }
    public string SalesPerson { get; set; }
    public string Status { get; set; }
    public string Subject { get; set; }
    public string Description { get; set; }
    public string DetailedDescription { get; set; }
    public string CreatedBy { get; set; }
    public string CreatedDate { get; set; }
    public string ModifiedBy { get; set; }
    public string ModifiedDate { get; set; }
}