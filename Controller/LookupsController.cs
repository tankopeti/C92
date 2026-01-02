// LookupsController.cs, új fájl létrehozva a partner és telephely lekérdezésekhez TomSelect komponenshez

using Cloud9_2.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cloud9_2.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class LookupsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LookupsController> _logger;

        public LookupsController(ApplicationDbContext context, ILogger<LookupsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ✅ Partner TomSelect (kereshető)
        // GET /api/Lookups/Partners?search=abc
        [HttpGet("Partners")]
        public async Task<IActionResult> Partners([FromQuery] string search = "")
        {
            try
            {
                search = (search ?? "").Trim();

                var q = _context.Partners.AsNoTracking().AsQueryable();

                // Feltételezés: Partner neve -> Name (ha nálad más, írd meg és átírom)
                if (!string.IsNullOrWhiteSpace(search))
                    q = q.Where(p => p.Name.Contains(search));

                var list = await q
                    .OrderBy(p => p.Name)
                    .Select(p => new { id = p.PartnerId, text = p.Name })
                    .Take(50)
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Partners lookup");
                return StatusCode(500, "Failed to load partners");
            }
        }

        // ✅ Site TomSelect partnerhez kötve (kereshető)
        // GET /api/Lookups/Sites?partnerId=12&search=abc
        [HttpGet("Sites")]
        public async Task<IActionResult> Sites([FromQuery] int partnerId, [FromQuery] string search = "")
        {
            try
            {
                search = (search ?? "").Trim();

                var q = _context.Sites.AsNoTracking()
                    .Where(s => s.PartnerId == partnerId);

                // Feltételezés: telephely neve -> SiteName (ha nálad más, írd meg és átírom)
                if (!string.IsNullOrWhiteSpace(search))
                    q = q.Where(s => s.SiteName.Contains(search));

                var list = await q
                    .OrderBy(s => s.SiteName)
                    .Select(s => new { id = s.SiteId, text = s.SiteName })
                    .Take(50)
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Sites lookup for partnerId={PartnerId}", partnerId);
                return StatusCode(500, "Failed to load sites");
            }
        }
    }
}
