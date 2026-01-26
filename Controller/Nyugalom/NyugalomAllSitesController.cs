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
            .Include(s => s.Partner)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            // Tel kereséshez: számokra normalizálás (opcionális, de hasznos)
            // pl. "+36 30 123 4567" -> "36301234567"
            var digits = new string(term.Where(char.IsDigit).ToArray());

            query = query.Where(s =>
                EF.Functions.Like(s.SiteName ?? "", $"%{term}%") ||
                EF.Functions.Like(s.City ?? "", $"%{term}%") ||
                EF.Functions.Like(s.AddressLine1 ?? "", $"%{term}%") ||

                // ✅ Site telefon mezők (NEVEKET IGAZÍTSD A MODELLEDHEZ!)
                EF.Functions.Like(s.Phone1 ?? "", $"%{term}%") ||
                EF.Functions.Like(s.MobilePhone1 ?? "", $"%{term}%") ||

                // ✅ digit-only keresés (ha a user csak számokat ír be)
                (!string.IsNullOrEmpty(digits) && (
                    EF.Functions.Like(
                        (s.Phone1 ?? "").Replace(" ", "").Replace("-", "").Replace("+", "").Replace("(", "").Replace(")", ""),
                        $"%{digits}%"
                    ) ||
                    EF.Functions.Like(
                        (s.MobilePhone1 ?? "").Replace(" ", "").Replace("-", "").Replace("+", "").Replace("(", "").Replace(")", ""),
                        $"%{digits}%"
                    )
                )) ||

                (s.Partner != null && (
                    EF.Functions.Like(s.Partner.CompanyNameTrim ?? "", $"%{term}%") ||
                    EF.Functions.Like(s.Partner.NameTrim ?? "", $"%{term}%") ||
                    EF.Functions.Like(s.Partner.TaxIdTrim ?? "", $"%{term}%") ||

                    // ✅ partner telefon (ha van ilyen meződ)
                    EF.Functions.Like(s.Partner.PhoneNumber ?? "", $"%{term}%") ||
                    EF.Functions.Like(s.Partner.Email ?? "", $"%{term}%")
                ))
            );
        }

        var sites = await query
            .OrderBy(s => s.SiteName)
            .ThenBy(s => s.City)
            .Take(300)
            .Select(s => new
            {
                id = s.SiteId,
                text = string.IsNullOrWhiteSpace(s.SiteName)
                    ? "Névtelen telephely"
                    : $"{s.SiteName} – {s.City ?? "nincs megadva"} – {s.AddressLine1 ?? "nincs megadva"}",

                partnerId = s.PartnerId,
                partnerName = s.Partner != null
                    ? (string.IsNullOrWhiteSpace(s.Partner.CompanyName)
                        ? (s.Partner.Name ?? "Nincs név")
                        : s.Partner.CompanyName)
                    : "Nincs partner",

                partnerDetails = s.Partner != null
                    ? $"{(string.IsNullOrWhiteSpace(s.Partner.CompanyName) ? s.Partner.Name : s.Partner.CompanyName)} {(string.IsNullOrWhiteSpace(s.Partner.TaxId) ? "" : $"({s.Partner.TaxId})")}".Trim()
                    : "Nincs partner",

                // ✅ TomSelect-hez: telefonszám mező
                // Prioritás: telephely telefon → partner telefon → üres
                phone = !string.IsNullOrWhiteSpace(s.Phone1) ? s.Phone1
                      : (!string.IsNullOrWhiteSpace(s.MobilePhone1) ? s.MobilePhone1
                      : (s.Partner != null ? s.Partner.PhoneNumber : ""))
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