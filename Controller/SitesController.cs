using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Cloud9_2.Controllers
{
    [Route("api/partners/{partnerId}/[controller]")]
    [ApiController]
    [Authorize]
    public class SitesController : ControllerBase
    {
        private readonly IPartnerService _partnerService;
        private readonly ILogger<SitesController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SitesController(IPartnerService partnerService, ILogger<SitesController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _partnerService = partnerService ?? throw new ArgumentNullException(nameof(partnerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        [HttpGet]
        public async Task<IActionResult> GetSites(int partnerId)
        {
            try
            {
                var partner = await _partnerService.GetPartnerAsync(partnerId);
                if (partner == null)
                {
                    _logger.LogWarning("Partner not found for PartnerId: {PartnerId}", partnerId);
                    return NotFound();
                }
                var sites = partner.Sites?.Select(s => new SiteDto
                {
                    SiteId = s.SiteId,
                    SiteName = s.SiteName,
                    AddressLine1 = s.AddressLine1,
                    AddressLine2 = s.AddressLine2,
                    City = s.City,
                    State = s.State,
                    PostalCode = s.PostalCode,
                    Country = s.Country,
                    IsPrimary = s.IsPrimary,
                    PartnerId = s.PartnerId,
                    StatusId = s.StatusId
                }).ToList() ?? new List<SiteDto>();
                return Ok(sites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sites for PartnerId: {PartnerId}", partnerId);
                return StatusCode(500, new { error = "Failed to fetch sites", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSite(int partnerId, [FromBody] SiteDto siteDto)
        {
            try
            {
                if (siteDto == null)
                {
                    _logger.LogWarning("CreateSite received null site data");
                    return BadRequest(new { error = "Site data is required" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("CreateSite validation failed: {Errors}", errors);
                    return BadRequest(new { error = "Validation failed", errors });
                }

                var site = await _partnerService.AddOrUpdateSiteAsync(partnerId, siteDto);
                _logger.LogInformation("Created site with SiteId: {SiteId} for PartnerId: {PartnerId} by User: {User}", 
                    site.SiteId, partnerId, _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System");
                return CreatedAtAction(nameof(GetSite), new { partnerId, id = site.SiteId }, site);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating site for PartnerId: {PartnerId}", partnerId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site for PartnerId: {PartnerId}", partnerId);
                return StatusCode(500, new { error = "Failed to create site", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSite(int partnerId, int id, [FromBody] SiteDto siteDto)
        {
            try
            {
                if (siteDto == null || id != siteDto.SiteId)
                {
                    _logger.LogWarning("UpdateSite received invalid site data or ID mismatch: {SiteDto}, ID: {Id}", siteDto, id);
                    return BadRequest(new { error = "ID mismatch or invalid site" });
                }

                var site = await _partnerService.AddOrUpdateSiteAsync(partnerId, siteDto);
                _logger.LogInformation("Updated site with SiteId: {SiteId} for PartnerId: {PartnerId} by User: {User}", 
                    site.SiteId, partnerId, _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System");
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating site for PartnerId: {PartnerId}, SiteId: {SiteId}", partnerId, id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site for PartnerId: {PartnerId}, SiteId: {SiteId}", partnerId, id);
                return StatusCode(500, new { error = "Failed to update site", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSite(int partnerId, int id)
        {
            try
            {
                var success = await _partnerService.DeleteSiteAsync(partnerId, id);
                if (!success)
                {
                    _logger.LogWarning("Site not found for deletion, PartnerId: {PartnerId}, SiteId: {SiteId}", partnerId, id);
                    return NotFound();
                }
                _logger.LogInformation("Deleted site with SiteId: {SiteId} for PartnerId: {PartnerId} by User: {User}", 
                    id, partnerId, _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site for PartnerId: {PartnerId}, SiteId: {SiteId}", partnerId, id);
                return StatusCode(500, new { error = "Failed to delete site", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSite(int partnerId, int id)
        {
            try
            {
                var partner = await _partnerService.GetPartnerAsync(partnerId);
                if (partner == null)
                {
                    _logger.LogWarning("Partner not found for PartnerId: {PartnerId}", partnerId);
                    return NotFound();
                }
                var site = partner.Sites?.FirstOrDefault(s => s.SiteId == id);
                if (site == null)
                {
                    _logger.LogWarning("Site not found for SiteId: {SiteId}", id);
                    return NotFound();
                }
                var siteDto = new SiteDto
                {
                    SiteId = site.SiteId,
                    SiteName = site.SiteName,
                    AddressLine1 = site.AddressLine1,
                    AddressLine2 = site.AddressLine2,
                    City = site.City,
                    State = site.State,
                    PostalCode = site.PostalCode,
                    Country = site.Country,
                    IsPrimary = site.IsPrimary,
                    PartnerId = site.PartnerId,
                    StatusId = site.StatusId
                };
                return Ok(siteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching site for PartnerId: {PartnerId}, SiteId: {SiteId}", partnerId, id);
                return StatusCode(500, new { error = "Failed to fetch site", details = ex.Message });
            }
        }
    }
}