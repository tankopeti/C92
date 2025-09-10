using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PartnersController : ControllerBase
    {
        private readonly IPartnerService _partnerService;
        private readonly ILogger<PartnersController> _logger;
        private readonly ApplicationDbContext _context;

        public PartnersController(IPartnerService partnerService, ILogger<PartnersController> logger, ApplicationDbContext context)
        {
            _partnerService = partnerService ?? throw new ArgumentNullException(nameof(partnerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ✅ API used by TomSelect
        // GET: api/partners/select?search=abc
        [HttpGet("select")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> GetPartnersForSelect([FromQuery] string search = "")
        {
            try
            {
                var partners = await _context.Partners
                    .AsNoTracking()
                    .Where(p => string.IsNullOrEmpty(search) ||
                                p.Name.Contains(search) ||
                                (p.CompanyName != null && p.CompanyName.Contains(search)))
                    .OrderBy(p => p.Name)
                    .Select(p => new
                    {
                        id = p.PartnerId,
                        text = p.Name + (p.CompanyName != null ? $" ({p.CompanyName})" : "")
                    })
                    .Take(50) // ✅ limit results for performance
                    .ToListAsync();

                _logger.LogInformation("Fetched {PartnerCount} partners for select", partners.Count);
                return Ok(partners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partners for select");
                return StatusCode(500, new { error = "Failed to retrieve partners for select" });
            }
        }

        // ✅ Standard paginated partner list (not used by TomSelect)
        // GET: api/partners?searchTerm=&statusFilter=&sortBy=createddate&page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetPartners(
            [FromQuery] string search = "",
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50)
        {
            try
            {
                var partners = await _context.Partners
                    .AsNoTracking()
                    .Where(p => string.IsNullOrEmpty(search) ||
                                p.Name.Contains(search) ||
                                (p.CompanyName != null && p.CompanyName.Contains(search)))
                    .OrderBy(p => p.Name)
                    .Skip(skip)
                    .Take(take)
                    .Select(p => new
                    {
                        id = p.PartnerId,
                        text = p.Name + (p.CompanyName != null ? $" ({p.CompanyName})" : "")
                    })
                    .ToListAsync();

                return Ok(partners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partners");
                return StatusCode(500, new { error = "Failed to retrieve partners" });
            }
        }

        // ✅ Single partner by id
        // GET: api/partners/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<PartnerDto>> GetPartner(int id)
        {
            try
            {
                var partner = await _partnerService.GetPartnerAsync(id);
                if (partner == null)
                {
                    _logger.LogWarning("Partner {PartnerId} not found", id);
                    return NotFound(new { error = $"Partner {id} not found" });
                }

                _logger.LogInformation("Fetched partner {PartnerId}: {PartnerName}", id, partner.Name);
                return Ok(partner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partner {PartnerId}", id);
                return StatusCode(500, new { error = "Failed to retrieve partner" });
            }
        }

        // 🔹 CREATE
        [HttpPost("CreatePartner")]
        public async Task<IActionResult> CreatePartner([FromBody] PartnerDto partnerDto)
        {
            if (partnerDto == null)
            {
                _logger.LogError("Received null partnerDto in CreatePartner");
                return BadRequest(new { message = "Partner data is null" });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogError("Invalid model state for partnerDto: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { message = "Invalid input data", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                var createdPartner = await _partnerService.CreatePartnerAsync(partnerDto);
                return CreatedAtAction(nameof(GetPartner), new { id = createdPartner.PartnerId }, createdPartner);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Validation error in CreatePartner: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error in CreatePartner: {Message}", ex.Message);
                return StatusCode(500, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreatePartner: {Message}", ex.Message);
                return StatusCode(500, new { message = "An unexpected error occurred", detail = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePartner(int id, [FromBody] Partner partner)
        {
            _logger.LogInformation("UpdatePartner called for PartnerId: {PartnerId}, Received Partner: {@Partner}", id, partner);

            if (partner == null)
            {
                _logger.LogWarning("UpdatePartner received null partner for PartnerId: {PartnerId}", id);
                return BadRequest(new { error = "Partner object is required" });
            }

            if (id != partner.PartnerId)
            {
                return BadRequest(new { error = "ID mismatch" });
            }

            _context.Entry(partner).State = EntityState.Modified;

            try
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(partner);
                if (!Validator.TryValidateObject(partner, validationContext, validationResults, true))
                {
                    var errors = validationResults.Select(r => new { Property = r.MemberNames.FirstOrDefault(), Message = r.ErrorMessage });
                    _logger.LogWarning("Validation failed for PartnerId: {PartnerId}, Errors: {@Errors}", id, errors);
                    return BadRequest(new { title = "One or more validation errors occurred", errors = errors.ToDictionary(e => e.Property, e => new[] { e.Message }) });
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Partners.Any(p => p.PartnerId == id))
                {
                    _logger.LogWarning("Partner not found for concurrency check, PartnerId: {PartnerId}", id);
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating partner for PartnerId: {PartnerId}", id);
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        // 🔹 COPY
        [HttpPost("{id}/copy")]
        public async Task<IActionResult> CopyPartner(int id)
        {
            var existing = await _context.Partners.FindAsync(id);
            if (existing == null)
                return NotFound();

            var newPartner = new Partner
            {
                Name = existing.Name + " (másolat)",
                CompanyName = existing.CompanyName,
                Email = existing.Email,
                PhoneNumber = existing.PhoneNumber,
                AddressLine1 = existing.AddressLine1,
                AddressLine2 = existing.AddressLine2,
                // stb… minden más másolható mező
            };

            _context.Partners.Add(newPartner);
            await _context.SaveChangesAsync();

            return Ok(newPartner);
        }

        // 🔹 DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePartner(int id)
        {
            var partner = await _context.Partners.FindAsync(id);
            if (partner == null)
                return NotFound();

            _context.Partners.Remove(partner);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        public async Task<IActionResult> OnPostCreatePartner([FromBody] PartnerDto partnerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");

            var partner = new Partner
            {
                Name = partnerDto.Name,
                PartnerGroupId = partnerDto.PartnerGroupId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Partners.Add(partner);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, partnerId = partner.PartnerId });
        }
    }




    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SitesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SitesController> _logger;

        public SitesController(ApplicationDbContext context, ILogger<SitesController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> CreateSite([FromBody] Site site)
        {
            if (site == null)
            {
                _logger.LogWarning("CreateSite received null site data");
                return BadRequest(new { error = "Site data is required" });
            }

            // Validate required fields with ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("CreateSite validation failed: {Errors}", errors);
                return BadRequest(new { error = "Validation failed", errors });
            }

            if (site.PartnerId <= 0)
            {
                _logger.LogWarning("CreateSite received invalid PartnerId: {PartnerId}", site.PartnerId);
                return BadRequest(new { error = "Valid PartnerId is required" });
            }

            var partner = await _context.Partners.FindAsync(site.PartnerId);
            if (partner == null)
            {
                _logger.LogWarning("Partner {PartnerId} not found for new site", site.PartnerId);
                return BadRequest(new { error = "Associated Partner not found" });
            }

            // Allow siteId to be omitted (auto-incremented by database)
            site.SiteId = 0; // Ensure auto-increment
            _context.Sites.Add(site);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created site with SiteId: {SiteId} for PartnerId: {PartnerId}", site.SiteId, site.PartnerId);
            return CreatedAtAction(nameof(GetSite), new { id = site.SiteId }, site);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSite(int id, [FromBody] Site site)
        {
            if (site == null || id != site.SiteId)
            {
                _logger.LogWarning("UpdateSite received invalid site data or ID mismatch: {Site}, ID: {Id}", site, id);
                return BadRequest(new { error = "ID mismatch or invalid site" });
            }

            var existingSite = await _context.Sites.FindAsync(id);
            if (existingSite == null)
            {
                _logger.LogWarning("Site {SiteId} not found for update", id);
                return NotFound();
            }

            _context.Entry(existingSite).CurrentValues.SetValues(site);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated site with SiteId: {SiteId}", id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSite(int id)
        {
            var site = await _context.Sites.FindAsync(id);
            if (site == null)
            {
                _logger.LogWarning("Site {SiteId} not found for deletion", id);
                return NotFound();
            }

            _context.Sites.Remove(site);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted site with SiteId: {SiteId}", id);
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSite(int id)
        {
            var site = await _context.Sites.FindAsync(id);
            if (site == null)
            {
                _logger.LogWarning("Site {SiteId} not found", id);
                return NotFound();
            }
            return Ok(site);
        }

        [HttpGet("by-partner/{partnerId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> GetSitesByPartner(int partnerId, [FromQuery] string search = "")
        {
            try
            {
                var sites = await _context.Sites
                    .AsNoTracking()
                    .Where(s => s.PartnerId == partnerId && 
                                (string.IsNullOrEmpty(search) || s.SiteName.Contains(search) || s.AddressLine1.Contains(search)))
                    .OrderBy(s => s.SiteName)
                    .Select(s => new
                    {
                        id = s.SiteId,
                        text = s.SiteName + (string.IsNullOrEmpty(s.AddressLine1) ? "" : $" ({s.AddressLine1})")
                    })
                    .Take(50) // Limit for performance
                    .ToListAsync();

                _logger.LogInformation("Fetched {SiteCount} sites for PartnerId {PartnerId}", sites.Count, partnerId);
                return Ok(sites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sites for PartnerId {PartnerId}", partnerId);
                return StatusCode(500, new { error = "Failed to retrieve sites" });
            }
        }

    }

}