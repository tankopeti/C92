using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CurrenciesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CurrenciesController> _logger;

        public CurrenciesController(ApplicationDbContext context, ILogger<CurrenciesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
public async Task<IActionResult> OnGetCurrenciesAsync([FromQuery] string? search = "")
        {
            try
            {
                _logger.LogInformation("Fetching currencies with search: '{Search}'", search ?? "null");
                var currencies = await _context.Currencies
                .Where(p => string.IsNullOrEmpty(search) ||
                            p.CurrencyName.Contains(search) ||
                            (p.CurrencyName != null && p.CurrencyName.Contains(search)))
                    .Select(c => new { id = c.CurrencyId, name = c.CurrencyName })
                    .Take(10)
                    .ToListAsync();
                _logger.LogInformation("Found {Count} currencies: {@Currencies}", currencies.Count, currencies);
                return new JsonResult(currencies) { StatusCode = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching currencies: {Message}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}