using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using System.Threading.Tasks;
using System.Linq;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartnersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PartnersController(ApplicationDbContext context)
        {
            _context = context;
        }

    [HttpGet]
    public async Task<IActionResult> GetPartners(string search = "", int skip = 0, int take = 50)
    {
        var partners = await _context.Partners
            .Include(p => p.Sites)
            .Include(p => p.Contacts)
            .Include(p => p.Quotes)
            .Where(p => string.IsNullOrEmpty(search) || 
                        p.Name.Contains(search) || 
                        (p.CompanyName != null && p.CompanyName.Contains(search)))
            .Skip(skip)
            .Take(take)
            .Select(p => new
            {
                id = p.PartnerId,
                text = p.Name + (p.CompanyName != null ? $" ({p.CompanyName})" : ""),
                sites = p.Sites.Select(s => new
                {
                    id = s.SiteId,
                    text = s.SiteName != null ? s.SiteName : 
                        (s.AddressLine1 != null ? $"{s.AddressLine1}, {s.City ?? "Unknown City"}" : "Unnamed Site"),
                    isPrimary = s.IsPrimary
                }).ToList(),
                contacts = p.Contacts.Select(c => new
                {
                    id = c.ContactId,
                    text = $"{c.FirstName} {c.LastName}" + (c.Email != null ? $" ({c.Email})" : ""),
                    isPrimary = c.IsPrimary
                }).ToList(),
                quotes = p.Quotes.Select(q => new
                {
                    id = q.QuoteId,
                    text = q.QuoteNumber ?? $"Quote {q.QuoteId}"
                }).ToList()
            })
            .ToListAsync();

        return Ok(partners);
    }
    }
}