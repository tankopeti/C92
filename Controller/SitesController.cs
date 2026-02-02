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

        public SitesIndexController(
            ApplicationDbContext context,
            PartnerService partnerService,
            ILogger<SitesIndexController> logger)
        {
            _context = context;
            _partnerService = partnerService;
            _logger = logger;
        }

        // GET: /api/SitesIndex?pageNumber=1&pageSize=50&search=...&filter=primary
        // filter: "" | "primary"
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
        (s.Country != null && s.Country.ToLower().Contains(term)) ||

        // kontakt
        (s.ContactPerson1 != null && s.ContactPerson1.ToLower().Contains(term)) ||
        (s.ContactPerson2 != null && s.ContactPerson2.ToLower().Contains(term)) ||
        (s.ContactPerson3 != null && s.ContactPerson3.ToLower().Contains(term)) ||

        // ✅ telefonok
        (s.Phone1 != null && s.Phone1.ToLower().Contains(term)) ||
        (s.Phone2 != null && s.Phone2.ToLower().Contains(term)) ||
        (s.Phone3 != null && s.Phone3.ToLower().Contains(term)) ||

        // ✅ mobilok
        (s.MobilePhone1 != null && s.MobilePhone1.ToLower().Contains(term)) ||
        (s.MobilePhone2 != null && s.MobilePhone2.ToLower().Contains(term)) ||
        (s.MobilePhone3 != null && s.MobilePhone3.ToLower().Contains(term)) ||

        // app + email
        (s.messagingApp1 != null && s.messagingApp1.ToLower().Contains(term)) ||
        (s.messagingApp2 != null && s.messagingApp2.ToLower().Contains(term)) ||
        (s.messagingApp3 != null && s.messagingApp3.ToLower().Contains(term)) ||
        (s.eMail1 != null && s.eMail1.ToLower().Contains(term)) ||
        (s.eMail2 != null && s.eMail2.ToLower().Contains(term)) ||

        // partner
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
                        country = s.Country,

                        isPrimary = s.IsPrimary,

                        contactPerson1 = s.ContactPerson1,
                        contactPerson2 = s.ContactPerson2,
                        contactPerson3 = s.ContactPerson3,

                        phone1 = s.Phone1,
                        phone2 = s.Phone2,
                        phone3 = s.Phone3,

                        mobilePhone1 = s.MobilePhone1,
                        mobilePhone2 = s.MobilePhone2,
                        mobilePhone3 = s.MobilePhone3,

                        messagingApp1 = s.messagingApp1,
                        messagingApp2 = s.messagingApp2,
                        messagingApp3 = s.messagingApp3,

                        eMail1 = s.eMail1,
                        eMail2 = s.eMail2,

                        comment1 = s.Comment1,
                        comment2 = s.Comment2,

                        statusId = s.StatusId,
                        status = s.Status == null ? null : new { id = s.Status.Id, name = s.Status.Name, color = s.Status.Color },

                        isActive = s.IsActive
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
        [HttpGet("{id:int}")]
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

                isPrimary = site.IsPrimary,

                contactPerson1 = site.ContactPerson1,
                contactPerson2 = site.ContactPerson2,
                contactPerson3 = site.ContactPerson3,

                phone1 = site.Phone1,
                phone2 = site.Phone2,
                phone3 = site.Phone3,

                mobilePhone1 = site.MobilePhone1,
                mobilePhone2 = site.MobilePhone2,
                mobilePhone3 = site.MobilePhone3,

                messagingApp1 = site.messagingApp1,
                messagingApp2 = site.messagingApp2,
                messagingApp3 = site.messagingApp3,

                eMail1 = site.eMail1,
                eMail2 = site.eMail2,

                comment1 = site.Comment1,
                comment2 = site.Comment2,

                statusId = site.StatusId,

                // view badge-hez
                status = site.Status == null ? null : new
                {
                    id = site.Status.Id,
                    name = site.Status.Name,
                    color = site.Status.Color
                },

                isActive = site.IsActive
            });
        }

        // POST: /api/SitesIndex  (AJAX create)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SiteDto dto)
        {
            if (dto == null)
                return BadRequest(new { title = "DTO is null" });

            if (!ModelState.IsValid)
                return BadRequest(new { title = "ModelState invalid", errors = ModelState });

            if (dto.PartnerId <= 0)
                return BadRequest(new { title = "Invalid input", errors = new { PartnerId = new[] { "Partner megadása kötelező" } } });

            // biztosítsuk, hogy create legyen
            dto.SiteId = 0;

            var created = await _partnerService.AddOrUpdateSiteAsync(dto.PartnerId, dto);

            // visszaadunk frissített sort (ugyanaz, mint Update-nél)
            var refreshed = await _context.Sites
                .AsNoTracking()
                .Include(s => s.Partner)
                .Include(s => s.Status)
                .Where(s => s.SiteId == created.SiteId)
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
                    isActive = s.IsActive,
                    statusId = s.StatusId,
                    status = s.Status == null ? null : new { id = s.Status.Id, name = s.Status.Name, color = s.Status.Color }
                })
                .FirstAsync();

            return Ok(refreshed);
        }


        // PUT: /api/SitesIndex/123  (AJAX edit, reload nélkül)
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] SiteDto dto)
        {
            if (dto == null)
                return BadRequest(new { title = "DTO is null" });

            if (!ModelState.IsValid)
                return BadRequest(new { title = "ModelState invalid", errors = ModelState });

            if (id != dto.SiteId)
                return BadRequest(new { title = "Invalid input", errors = new { Id = new[] { "ID mismatch" } } });

            // partnerId kell a PartnerService-hez
            var partnerId = await _context.Sites
                .Where(s => s.SiteId == id)
                .Select(s => s.PartnerId)
                .FirstOrDefaultAsync();

            if (partnerId == 0)
                return NotFound(new { title = "Not found", errors = new { Id = new[] { "Site not found" } } });

            // Meglévő logikád: PartnerService AddOrUpdateSiteAsync
            var updated = await _partnerService.AddOrUpdateSiteAsync(partnerId, dto);

            // Friss visszaadott adat (JS patch-hez)
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
                    country = s.Country,

                    isPrimary = s.IsPrimary,

                    contactPerson1 = s.ContactPerson1,
                    contactPerson2 = s.ContactPerson2,
                    contactPerson3 = s.ContactPerson3,

                    phone1 = s.Phone1,
                    phone2 = s.Phone2,
                    phone3 = s.Phone3,

                    mobilePhone1 = s.MobilePhone1,
                    mobilePhone2 = s.MobilePhone2,
                    mobilePhone3 = s.MobilePhone3,

                    messagingApp1 = s.messagingApp1,
                    messagingApp2 = s.messagingApp2,
                    messagingApp3 = s.messagingApp3,

                    eMail1 = s.eMail1,
                    eMail2 = s.eMail2,

                    comment1 = s.Comment1,
                    comment2 = s.Comment2,

                    statusId = s.StatusId,
                    status = s.Status == null ? null : new { id = s.Status.Id, name = s.Status.Name, color = s.Status.Color },

                    isActive = s.IsActive
                })
                .FirstAsync();

            return Ok(refreshed);
        }

        // DELETE: /api/SitesIndex/123
        [HttpDelete("{id:int}")]
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
                    .Where(s => s.IsActive == true && s.PartnerId == partnerId);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(s =>
                        (s.SiteName != null && s.SiteName.ToLower().Contains(term)) ||
                        (s.City != null && s.City.ToLower().Contains(term)) ||
                        (s.AddressLine1 != null && s.AddressLine1.ToLower().Contains(term)) ||
                        (s.PostalCode != null && s.PostalCode.ToLower().Contains(term))
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
                _logger.LogError(ex, "Error fetching sites for partner {PartnerId}", partnerId);

                return StatusCode(500, new { error = "Failed to retrieve sites" });
            }
        }
    }
}
