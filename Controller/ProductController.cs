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
        public async Task<IActionResult> GetProducts([FromQuery] string? search = "")
        {
            _logger.LogInformation("GetProducts called with search: {search}", search);
            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => EF.Functions.Like(p.Name, $"%{search}%"));
            }
            var products = await query
                .Select(p => new { id = p.ProductId, name = p.Name })
                .ToListAsync();
            _logger.LogInformation("Returning {count} products", products.Count);
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            _logger.LogInformation("GetProduct called with id: {id}", id);
            var product = await _context.Products
                .Where(p => p.ProductId == id)
                .Select(p => new { id = p.ProductId, name = p.Name })
                .FirstOrDefaultAsync();
            if (product == null)
            {
                _logger.LogWarning("Product not found for id: {id}", id);
                return NotFound();
            }
            return Ok(product);
        }
    }
}