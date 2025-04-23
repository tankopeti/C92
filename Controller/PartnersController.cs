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
        public async Task<IActionResult> GetPartners(string search = "")
        {
            var partners = await _context.Partners
                .Where(p => string.IsNullOrEmpty(search) || 
                            p.Name.Contains(search) || 
                            (p.CompanyName != null && p.CompanyName.Contains(search)))
                .Select(p => new {
                    id = p.PartnerId,
                    text = p.Name + (p.CompanyName != null ? $" ({p.CompanyName})" : "")
                })
                .ToListAsync();
            return Ok(partners);
        }
      }
  }