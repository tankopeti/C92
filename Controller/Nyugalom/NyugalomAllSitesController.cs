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
                var query = _context.Sites
                    .AsNoTracking()
                    .Include(s => s.Partner) // Partner betöltése (Name, CompanyName miatt)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();

                    query = query.Where(s =>
                        EF.Functions.Like(s.SiteName ?? "", $"%{term}%") ||
                        EF.Functions.Like(s.City ?? "", $"%{term}%") ||
                        EF.Functions.Like(s.AddressLine1 ?? "", $"%{term}%") ||
                        (s.Partner != null && (
                            EF.Functions.Like(s.Partner.CompanyNameTrim ?? "", $"%{term}%") ||
                            EF.Functions.Like(s.Partner.NameTrim ?? "", $"%{term}%") ||
                            EF.Functions.Like(s.Partner.TaxIdTrim ?? "", $"%{term}%")
                        ))
                    );
                }


                var sites = await query
                    .OrderBy(s => s.SiteName)
                    .ThenBy(s => s.City)
                    .Take(300) // MSSQL-en 500 sok lehet, 300 még biztonságos és gyors
                    .Select(s => new
                    {
                        id = s.SiteId,
                        text = string.IsNullOrWhiteSpace(s.SiteName)
                            ? "Névtelen telephely"
                            : $"{s.SiteName} – {s.City ?? "nincs megadva"} – {s.AddressLine1 ?? "nincs megadva"}",

                        // Ezek lesznek elérhetőek a TomSelect-ben
                        partnerId = s.PartnerId,
                        partnerName = s.Partner != null
                            ? string.IsNullOrWhiteSpace(s.Partner.CompanyName)
                                ? s.Partner.Name ?? "Nincs név"
                                : s.Partner.CompanyName
                            : "Nincs partner",

                        // Extra: ha szeretnéd, add vissza a partner teljes nevét + adószámot is
                        partnerDetails = s.Partner != null
                            ? $"{(string.IsNullOrWhiteSpace(s.Partner.CompanyName) ? s.Partner.Name : s.Partner.CompanyName)} {(string.IsNullOrWhiteSpace(s.Partner.TaxId) ? "" : $"({s.Partner.TaxId})")}".Trim()
                            : "Nincs partner"
                    })
                    .ToListAsync();

                return Ok(sites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hiba a telephelyek betöltésekor (TomSelect API)");
                return StatusCode(500, new { message = "Szerveroldali hiba történt a telephelyek betöltése közben." });
            }
        }

    }
}