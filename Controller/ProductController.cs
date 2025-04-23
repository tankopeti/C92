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
        public async Task<IActionResult> GetProducts([FromQuery] string search)
        {
            _logger.LogInformation($"GetProducts called with search term: {search}");
            try
            {
                if (_context.Products == null)
                {
                    _logger.LogError("Products DbSet is null");
                    return StatusCode(500, "Products DbSet is not configured in ApplicationDbContext");
                }

                var query = _context.Products
                    .Where(p => p.IsActive)
                    .AsQueryable();

                _logger.LogInformation("Building product query");

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || (p.SKU != null && p.SKU.Contains(search)));
                }

                var products = await query
                    .OrderBy(p => p.Name)
                    .Take(10)
                    .Select(p => new
                    {
                        id = p.ProductId,
                        text = p.Name,
                        sku = p.SKU
                    })
                    .ToListAsync();

                _logger.LogInformation($"Found {products.Count} products");
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetProducts");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            _logger.LogInformation("ProductController test endpoint called");
            return Ok("ProductController is reachable");
        }
    }
}