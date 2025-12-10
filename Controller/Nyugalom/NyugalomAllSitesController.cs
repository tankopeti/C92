using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;

namespace Cloud9_2.Controllers.Nyugalom
{
    [Route("api/nyugalom/sites")]
    [ApiController]
    [Authorize]
    public class NyugalomSitesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NyugalomSitesController> _logger;

        public NyugalomSitesController(ApplicationDbContext context, ILogger<NyugalomSitesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("all/select")]
        public async Task<IActionResult> GetAllSitesForSelect([FromQuery] string search = "")
        {
            try
            {
                var query = _context.Sites.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    query = query.Where(s =>
                        EF.Functions.Like(s.SiteName ?? "", $"%{term}%") ||
                        EF.Functions.Like(s.City ?? "", $"%{term}%") ||
                        EF.Functions.Like(s.AddressLine1 ?? "", $"%{term}%")
                    );
                }

                var sites = await query
                    .OrderBy(s => s.SiteName)
                    .Take(500)
                    .Select(s => new
                    {
                        id = s.SiteId,
                        text = string.IsNullOrEmpty(s.SiteName)
                            ? "Névtelen telephely"
                            : $"{s.SiteName} ({s.City ?? "Ismeretlen város"})"
                    })
                    .ToListAsync();

                return Ok(sites);
            }
            catch (Exception ex)
            {
            _logger.LogError(ex, "Nyugalom: telephelyek betöltése sikertelen");
                return StatusCode(500, "Hiba a telephelyek betöltésekor");
            }
        }
    }
}