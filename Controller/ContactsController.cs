using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http; // Add for IHttpContextAccessor

namespace Cloud9_2.Controllers
{
    [Route("api/partners/{partnerId}/[controller]")]
    [ApiController]
    [Authorize]
    public class ContactsController : ControllerBase
    {
        private readonly IPartnerService _partnerService;
        private readonly ILogger<ContactsController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ContactsController(IPartnerService partnerService, ILogger<ContactsController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _partnerService = partnerService ?? throw new ArgumentNullException(nameof(partnerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        [HttpGet]
        public async Task<IActionResult> GetContacts(int partnerId)
        {
            try
            {
                var partner = await _partnerService.GetPartnerAsync(partnerId);
                if (partner == null)
                {
                    _logger.LogWarning("Partner not found for PartnerId: {PartnerId}", partnerId);
                    return NotFound();
                }
                var contacts = partner.Contacts?.Select(c => new ContactDto
                {
                    ContactId = c.ContactId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber,
                    JobTitle = c.JobTitle,
                    Comment = c.Comment,
                    IsPrimary = c.IsPrimary
                }).ToList() ?? new List<ContactDto>();
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contacts for PartnerId: {PartnerId}", partnerId);
                return StatusCode(500, new { error = "Failed to fetch contacts", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateContact(int partnerId, [FromBody] ContactDto contactDto)
        {
            try
            {
                if (contactDto == null)
                {
                    _logger.LogWarning("CreateContact received null contact data");
                    return BadRequest(new { error = "Contact data is required" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("CreateContact validation failed: {Errors}", errors);
                    return BadRequest(new { error = "Validation failed", errors });
                }

                var contact = await _partnerService.AddOrUpdateContactAsync(partnerId, contactDto);
                _logger.LogInformation("Created contact with ContactId: {ContactId} for PartnerId: {PartnerId} by User: {User}", 
                    contact.ContactId, partnerId, _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System");
                return CreatedAtAction(nameof(GetContact), new { partnerId, id = contact.ContactId }, contact);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating contact for PartnerId: {PartnerId}", partnerId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact for PartnerId: {PartnerId}", partnerId);
                return StatusCode(500, new { error = "Failed to create contact", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContact(int partnerId, int id, [FromBody] ContactDto contactDto)
        {
            try
            {
                if (contactDto == null || id != contactDto.ContactId)
                {
                    _logger.LogWarning("UpdateContact received invalid contact data or ID mismatch: {ContactDto}, ID: {Id}", contactDto, id);
                    return BadRequest(new { error = "ID mismatch or invalid contact" });
                }

                var contact = await _partnerService.AddOrUpdateContactAsync(partnerId, contactDto);
                _logger.LogInformation("Updated contact with ContactId: {ContactId} for PartnerId: {PartnerId} by User: {User}", 
                    contact.ContactId, partnerId, _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System");
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating contact for PartnerId: {PartnerId}, ContactId: {ContactId}", partnerId, id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact for PartnerId: {PartnerId}, ContactId: {ContactId}", partnerId, id);
                return StatusCode(500, new { error = "Failed to update contact", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int partnerId, int id)
        {
            try
            {
                var success = await _partnerService.DeleteContactAsync(partnerId, id);
                if (!success)
                {
                    _logger.LogWarning("Contact not found for deletion, PartnerId: {PartnerId}, ContactId: {ContactId}", partnerId, id);
                    return NotFound();
                }
                _logger.LogInformation("Deleted contact with ContactId: {ContactId} for PartnerId: {PartnerId} by User: {User}", 
                    id, partnerId, _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact for PartnerId: {PartnerId}, ContactId: {ContactId}", partnerId, id);
                return StatusCode(500, new { error = "Failed to delete contact", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetContact(int partnerId, int id)
        {
            try
            {
                var partner = await _partnerService.GetPartnerAsync(partnerId);
                if (partner == null)
                {
                    _logger.LogWarning("Partner not found for PartnerId: {PartnerId}", partnerId);
                    return NotFound();
                }
                var contact = partner.Contacts?.FirstOrDefault(c => c.ContactId == id);
                if (contact == null)
                {
                    _logger.LogWarning("Contact not found for ContactId: {ContactId}", id);
                    return NotFound();
                }
                return Ok(contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contact for PartnerId: {PartnerId}, ContactId: {ContactId}", partnerId, id);
                return StatusCode(500, new { error = "Failed to fetch contact", details = ex.Message });
            }
        }
    }
}