using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SitesIndexController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PartnerService _partnerService;
        private readonly ILogger<SitesIndexController> _logger;

        public SitesIndexController(ApplicationDbContext context, PartnerService partnerService, ILogger<SitesIndexController> logger)
        {
            _context = context;
            _partnerService = partnerService;
            _logger = logger;
        }

        // GET: /api/SitesIndex?pageNumber=1&pageSize=50&search=...&filter=primary
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string search = "",
            [FromQuery] string filter = "")
        {
            try
            {
                pageNumber = pageNumber < 1 ? 1 : pageNumber;
                pageSize = pageSize < 1 ? 50 : pageSize;

                var q = _context.Sites
                    .AsNoTracking()
                    .Include(s => s.Partner)
                    .Include(s => s.Status)
                    .Where(s => s.IsActive == true);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();

                    q = q.Where(s =>
                        (s.SiteName != null && s.SiteName.ToLower().Contains(term)) ||
                        (s.City != null && s.City.ToLower().Contains(term)) ||
                        (s.AddressLine1 != null && s.AddressLine1.ToLower().Contains(term)) ||
                        (s.AddressLine2 != null && s.AddressLine2.ToLower().Contains(term)) ||
                        (s.PostalCode != null && s.PostalCode.ToLower().Contains(term)) ||
                        (s.Partner != null && (
                            (s.Partner.Name != null && s.Partner.Name.ToLower().Contains(term)) ||
                            (s.Partner.CompanyName != null && s.Partner.CompanyName.ToLower().Contains(term))
                        ))
                    );
                }

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    if (filter == "primary")
                        q = q.Where(s => s.IsPrimary == true);
                }

                var total = await q.CountAsync();

                var data = await q
                    .OrderByDescending(s => s.LastModifiedDate ?? s.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        siteId = s.SiteId,
                        siteName = s.SiteName,
                        partnerId = s.PartnerId,
                        partnerName = s.Partner != null
                            ? (string.IsNullOrWhiteSpace(s.Partner.CompanyName) ? s.Partner.Name : s.Partner.CompanyName)
                            : null,
                        addressLine1 = s.AddressLine1,
                        addressLine2 = s.AddressLine2,
                        city = s.City,
                        state = s.State,
                        postalCode = s.PostalCode,
                        contactPerson1 = s.ContactPerson1,
                        contactPerson2 = s.ContactPerson2,
                        contactPerson3 = s.ContactPerson3,
                        isPrimary = s.IsPrimary,
                        statusId = s.StatusId,
                        status = s.Status == null ? null : new { id = s.Status.Id, name = s.Status.Name, color = s.Status.Color }
                    })
                    .ToListAsync();

                Response.Headers["X-Total-Count"] = total.ToString();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sites index");
                return StatusCode(500, new { success = false, message = "Error retrieving sites" });
            }
        }

// GET: /api/SitesIndex/123  -> részletek (view/edit)
[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    var site = await _context.Sites
        .AsNoTracking()
        .Include(s => s.Partner)
        .Include(s => s.Status)
        .FirstOrDefaultAsync(s => s.SiteId == id && s.IsActive == true);

    if (site == null) return NotFound(new { title = "Not found" });

    return Ok(new
    {
        siteId = site.SiteId,
        siteName = site.SiteName,

        partnerId = site.PartnerId,
        partnerName = site.Partner != null
            ? (string.IsNullOrWhiteSpace(site.Partner.CompanyName) ? site.Partner.Name : site.Partner.CompanyName)
            : null,

        addressLine1 = site.AddressLine1,
        addressLine2 = site.AddressLine2,
        city = site.City,
        state = site.State,
        postalCode = site.PostalCode,
        country = site.Country,

        contactPerson1 = site.ContactPerson1,
        contactPerson2 = site.ContactPerson2,
        contactPerson3 = site.ContactPerson3,
        comment1 = site.Comment1,
        comment2 = site.Comment2,

        isPrimary = site.IsPrimary,
        statusId = site.StatusId,

        // ✅ EZ KELL a view badge-hez
        status = site.Status == null ? null : new
        {
            id = site.Status.Id,
            name = site.Status.Name,
            color = site.Status.Color
        },

        isActive = site.IsActive
    });
}


        // PUT: /api/SitesIndex/123  (AJAX edit, reload nélkül)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SiteDto dto)
        {
            if (dto == null)
            return BadRequest(new { title = "DTO is null" });

            if (!ModelState.IsValid)
            return BadRequest(new { title = "ModelState invalid", errors = ModelState });

            if (dto == null || id != dto.SiteId) return BadRequest(new { title = "Invalid input", errors = new { Id = new[] { "ID mismatch" } } });

            // szükségünk van partnerId-ra a partnerService-hez
            var partnerId = await _context.Sites
                .Where(s => s.SiteId == id)
                .Select(s => s.PartnerId)
                .FirstOrDefaultAsync();

            if (partnerId == 0) return NotFound(new { title = "Not found", errors = new { Id = new[] { "Site not found" } } });

            // meglévő logikád: PartnerService AddOrUpdateSiteAsync
            var updated = await _partnerService.AddOrUpdateSiteAsync(partnerId, dto);

            // visszaadunk egy friss DTO-t, hogy JS patch-elni tudjon
            var refreshed = await _context.Sites
                .AsNoTracking()
                .Include(s => s.Partner)
                .Include(s => s.Status)
                .Where(s => s.SiteId == updated.SiteId)
                .Select(s => new
                {
                    siteId = s.SiteId,
                    siteName = s.SiteName,
                    partnerId = s.PartnerId,
                    partnerName = s.Partner != null
                        ? (string.IsNullOrWhiteSpace(s.Partner.CompanyName) ? s.Partner.Name : s.Partner.CompanyName)
                        : null,
                    addressLine1 = s.AddressLine1,
                    addressLine2 = s.AddressLine2,
                    city = s.City,
                    state = s.State,
                    postalCode = s.PostalCode,
                    contactPerson1 = s.ContactPerson1,
                    contactPerson2 = s.ContactPerson2,
                    contactPerson3 = s.ContactPerson3,
                    isPrimary = s.IsPrimary,
                    statusId = s.StatusId,
                    status = s.Status == null ? null : new { id = s.Status.Id, name = s.Status.Name, color = s.Status.Color }
                })
                .FirstAsync();

            return Ok(refreshed);
        }

        // DELETE: /api/SitesIndex/123
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var partnerId = await _context.Sites
                .Where(s => s.SiteId == id)
                .Select(s => s.PartnerId)
                .FirstOrDefaultAsync();

            if (partnerId == 0) return NotFound();

            var ok = await _partnerService.DeleteSiteAsync(partnerId, id);
            if (!ok) return NotFound();

            return NoContent();
        }
    }


        [ApiController]
    [Route("api/sites")]
    [Authorize]
    public class SitesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SitesController> _logger;

        public SitesController(
            ApplicationDbContext context,
            ILogger<SitesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /api/sites/by-partner/{partnerId}?search=
        [HttpGet("by-partner/{partnerId:int}")]
        public async Task<IActionResult> GetByPartner(
            int partnerId,
            [FromQuery] string search = "")
        {
            try
            {
                if (partnerId <= 0)
                    return BadRequest(new { error = "Invalid partnerId" });

                search ??= "";

                var query = _context.Sites
                    .AsNoTracking()
                    .Where(s =>
                        s.IsActive == true &&
                        s.PartnerId == partnerId);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(s =>
                        (s.SiteName != null && s.SiteName.ToLower().Contains(term)) ||
                        (s.City != null && s.City.ToLower().Contains(term)) ||
                        (s.AddressLine1 != null && s.AddressLine1.ToLower().Contains(term))
                    );
                }

                var result = await query
                    .OrderBy(s => s.SiteName)
                    .Take(100)
                    .Select(s => new
                    {
                        id = s.SiteId,
                        text = s.SiteName
                    })
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching sites for partner {PartnerId}", partnerId);

                return StatusCode(500, new
                {
                    error = "Failed to retrieve sites"
                });
            }
        }
    }
}
