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
        public async Task<IActionResult> OnGetProductsAsync([FromQuery] string? search = "")
        {
            var products = await _context.Products
                .Where(p => string.IsNullOrEmpty(search) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Select(p => new { id = p.ProductId, name = p.Name })
                .ToListAsync();
            return Ok(products);
        }
    }
}