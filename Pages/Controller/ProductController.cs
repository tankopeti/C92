using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;

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
                    _logger.LogInformation("Applying search filter: {search}", search);
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
                            .FirstOrDefault() ?? p.UnitPrice,
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
                                .FirstOrDefault() ?? p.UnitPrice : null
                    })
                    .ToListAsync();

                // Log PartnerPrice query results
                if (partnerId.HasValue)
                {
                    var partnerPriceProducts = await _context.PartnerProductPrice
                        .Where(ppp => ppp.PartnerId == partnerId.Value)
                        .Select(ppp => new { ppp.ProductId, ppp.PartnerUnitPrice })
                        .ToListAsync();
                    _logger.LogInformation("PartnerProductPrice entries for partnerId: {partnerId}: {entries}", partnerId, string.Join(", ", partnerPriceProducts.Select(p => $"ProductId: {p.ProductId}, Price: {p.PartnerUnitPrice}")));
                }

                // Log the product IDs returned
                var productIds = products.Select(p => p.ProductId).ToList();
                _logger.LogInformation("Found {count} products with IDs: {productIds}", products.Count, string.Join(", ", productIds));

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
                            product.VolumePrice = product.ListPrice;
                        }
                    }
                    else
                    {
                        product.VolumePrice = product.ListPrice;
                    }
                    // Ensure PartnerPrice defaults to ListPrice if null or 0
                    if (partnerId.HasValue && (product.PartnerPrice == null || product.PartnerPrice == 0))
                    {
                        _logger.LogWarning("No valid PartnerPrice for ProductId: {productId}, PartnerId: {partnerId}, using ListPrice: {listPrice}", product.ProductId, partnerId, product.ListPrice);
                        product.PartnerPrice = product.ListPrice;
                    }
                }

                if (!products.Any())
                {
                    _logger.LogWarning("No products found for partnerId: {partnerId}, search: {search}, quoteDate: {quoteDate}, quantity: {quantity}", partnerId, search, quoteDate, quantity);
                    return NotFound("No products found");
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
        public async Task<IActionResult> GetProductById(int id, [FromQuery] int? partnerId = null, [FromQuery] DateTime? quoteDate = null, [FromQuery] int quantity = 1)
        {
            try
            {
                _logger.LogInformation("GetProductById called with id: {id}, partnerId: {partnerId}, quoteDate: {quoteDate}, quantity: {quantity}", id, partnerId, quoteDate, quantity);
                var product = await _context.Products
                    .Where(p => p.ProductId == id)
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
                            .FirstOrDefault() ?? p.UnitPrice,
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
                                .FirstOrDefault() ?? p.UnitPrice : null
                    })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    _logger.LogWarning("Product not found for id: {id}, partnerId: {partnerId}", id, partnerId);
                    return NotFound("Product not found");
                }

                // Apply volume-based pricing logic
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
                        product.VolumePrice = product.ListPrice;
                    }
                }
                else
                {
                    product.VolumePrice = product.ListPrice;
                }

                // Ensure PartnerPrice defaults to ListPrice if null or 0
                if (partnerId.HasValue && (product.PartnerPrice == null || product.PartnerPrice == 0))
                {
                    _logger.LogWarning("No valid PartnerPrice for ProductId: {productId}, PartnerId: {partnerId}, using ListPrice: {listPrice}", product.ProductId, partnerId, product.ListPrice);
                    product.PartnerPrice = product.ListPrice;
                }

                _logger.LogInformation("Returning product with id: {id}", id);
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProductById for id: {id}, partnerId: {partnerId}", id, partnerId);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        [HttpGet("partner-price")]
        public async Task<IActionResult> GetPartnerProductPrice([FromQuery] int partnerId, [FromQuery] int productId)
        {
            var product = await _context.Products
                .Where(p => p.ProductId == productId)
                .Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    PartnerPrice = _context.PartnerProductPrice
                        .Where(ppp => ppp.ProductId == productId && ppp.PartnerId == partnerId)
                        .Select(ppp => (decimal?)ppp.PartnerUnitPrice)
                        .FirstOrDefault() ?? p.UnitPrice
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpGet("pricing/{productId}")]
        public async Task<IActionResult> GetVolumePricing(int productId)
        {
            var pricing = await _context.ProductPrices
                .Where(p => p.ProductId == productId && p.IsActive)
                .Select(p => new
                {
                    p.ProductId,
                    p.SalesPrice,
                    p.Volume1,
                    p.Volume1Price,
                    p.Volume2,
                    p.Volume2Price,
                    p.Volume3,
                    p.Volume3Price
                })
                .FirstOrDefaultAsync();

            if (pricing == null)
                return NotFound($"Nincs aktív volume pricing a termékhez (ID: {productId})");

            return Ok(pricing);
        }

    }
}
