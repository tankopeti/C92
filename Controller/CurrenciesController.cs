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
                    text = c.CurrencyName,
                    currencyCode = c.CurrencyCode,
                    locale = c.Locale,
                    exchangeRate = c.ExchangeRate,
                    isBaseCurrency = c.IsBaseCurrency
                })
                .ToListAsync();
            return Ok(currencies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCurrencyById(int id)
        {
            var currency = await _context.Currencies.FindAsync(id);
            if (currency == null)
            {
                return NotFound();
            }
            return Ok(new
            {
                id = currency.CurrencyId,
                text = currency.CurrencyName,
                currencyCode = currency.CurrencyCode,
                locale = currency.Locale,
                exchangeRate = currency.ExchangeRate,
                isBaseCurrency = currency.IsBaseCurrency
            });
        }
    }
}