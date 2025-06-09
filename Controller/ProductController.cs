using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _logger = logger;
            _context = context;
            _logger.LogInformation("ProductController instantiated");
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] string? search = null, [FromQuery] int? partnerId = null, [FromQuery] DateTime? quoteDate = null, [FromQuery] int quantity = 1)
        {
            try
            {
                _logger.LogInformation("GetProducts called with search: {search}, partnerId: {partnerId}, quoteDate: {quoteDate}, quantity: {quantity}", search, partnerId, quoteDate, quantity);
                var query = _context.Products.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => EF.Functions.Like(p.Name, $"%{search}%"));
                }

                var products = await query
                    .Select(p => new ProductDto
                    {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        UnitPrice = p.UnitPrice,
                        ListPrice = _context.ProductPrices
                            .Where(pp => pp.ProductId == p.ProductId && pp.IsActive
                                && (quoteDate == null || pp.StartDate <= quoteDate)
                                && (quoteDate == null || pp.EndDate == null || pp.EndDate >= quoteDate))
                            .OrderByDescending(pp => pp.StartDate)
                            .Select(pp => (decimal?)pp.SalesPrice)
                            .FirstOrDefault() ?? p.UnitPrice, // Default to unitPrice if no valid price found
                        VolumePricing = _context.ProductPrices
                            .Where(pp => pp.ProductId == p.ProductId && pp.IsActive
                                && (quoteDate == null || pp.StartDate <= quoteDate)
                                && (quoteDate == null || pp.EndDate == null || pp.EndDate >= quoteDate))
                            .OrderByDescending(pp => pp.StartDate)
                            .Select(pp => new VolumePricing
                            {
                                Volume1 = pp.Volume1,
                                Volume1Price = (decimal?)pp.Volume1Price,
                                Volume2 = pp.Volume2,
                                Volume2Price = (decimal?)pp.Volume2Price,
                                Volume3 = pp.Volume3,
                                Volume3Price = (decimal?)pp.Volume3Price
                            })
                            .FirstOrDefault() ?? new VolumePricing(),
                        PartnerPrice = partnerId.HasValue ?
                            _context.PartnerProductPrice
                                .Where(ppp => ppp.ProductId == p.ProductId && ppp.PartnerId == partnerId.Value)
                                .Select(ppp => (decimal?)ppp.PartnerUnitPrice)
                                .FirstOrDefault() : null
                    })
                    .ToListAsync();

                // Apply volume-based pricing logic
                foreach (var product in products)
                {
                    if (product.VolumePricing != null)
                    {
                        if (quantity <= product.VolumePricing.Volume1)
                        {
                            product.VolumePrice = product.VolumePricing.Volume1Price ?? product.ListPrice;
                        }
                        else if (quantity > product.VolumePricing.Volume1 && quantity <= product.VolumePricing.Volume2)
                        {
                            product.VolumePrice = product.VolumePricing.Volume2Price ?? product.ListPrice;
                        }
                        else if (quantity > product.VolumePricing.Volume2 && quantity <= product.VolumePricing.Volume3)
                        {
                            product.VolumePrice = product.VolumePricing.Volume3Price ?? product.ListPrice;
                        }
                        else
                        {
                            product.VolumePrice = product.ListPrice; // Default to listPrice if no volume price applies
                        }
                    }
                    else
                    {
                        product.VolumePrice = product.ListPrice; // Default to listPrice if no volume pricing exists
                    }
                }

                _logger.LogInformation("Returning {count} products", products.Count);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProducts for partnerId: {partnerId}, quoteDate: {quoteDate}, quantity: {quantity}", partnerId, quoteDate, quantity);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id, [FromQuery] int? partnerId = null)
        {
            try
            {
                _logger.LogInformation("GetProductById called with id: {id}, partnerId: {partnerId}", id, partnerId);
                var product = await _context.Products
                    .Where(p => p.ProductId == id)
                    .Select(p => new
                    {
                        id = p.ProductId,
                        name = p.Name,
                        unitPrice = p.UnitPrice,
                        listPrice = _context.ProductPrices
                            .Where(pp => pp.ProductId == p.ProductId && pp.IsActive)
                            .OrderByDescending(pp => pp.StartDate) // Get the latest price
                            .Select(pp => pp.SalesPrice)
                            .FirstOrDefault(), // Get SalesPrice from ProductPrice
                        partnerPrice = partnerId.HasValue ?
                            _context.PartnerProductPrice
                                .Where(ppp => ppp.ProductId == p.ProductId && ppp.PartnerId == partnerId.Value)
                                .Select(ppp => (decimal?)ppp.PartnerUnitPrice)
                                .FirstOrDefault() : null
                            })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    _logger.LogWarning("Product not found for id: {id}", id);
                    return NotFound();
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProductById for id: {id}, partnerId: {partnerId}", id, partnerId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
