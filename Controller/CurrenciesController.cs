using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CurrenciesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CurrenciesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrencies(string term = "")
        {
            var currencies = await _context.Currencies
                .Where(c => string.IsNullOrEmpty(term) || c.CurrencyName.Contains(term))
                .Select(c => new
                {
                    id = c.CurrencyId,
                    text = c.CurrencyName
                })
                .ToListAsync();
            return Ok(currencies);
        }

        // [HttpGet("{id}")]
        // public async Task<IActionResult> GetCurrency(int id)
        // {
        //     var currency = await _context.Currencies
        //         .Where(c => c.CurrencyId == id)
        //         .Select(c => new
        //         {
        //             id = c.CurrencyId,
        //             text = c.CurrencyName
        //         })
        //         .FirstOrDefaultAsync();
        //     if (currency == null)
        //     {
        //         return NotFound(new { message = $"Currency with ID {id} not found" });
        //     }
        //     return Ok(currency);
        // }
    }
}