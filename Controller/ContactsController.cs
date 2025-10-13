using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Added for AsNoTracking
using Microsoft.Extensions.Logging; // Added for ILogger
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContactController : ControllerBase
    {
        private readonly ContactService _service;
        private readonly ApplicationDbContext _context; // Added
        private readonly ILogger<ContactController> _logger; // Added

        public ContactController(ContactService service, ApplicationDbContext context, ILogger<ContactController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/partners/{partnerId}/contacts/select?search=abc
        [HttpGet("/api/partners/{partnerId}/contacts/select")]
        public async Task<IActionResult> GetContactsForSelect(int partnerId, [FromQuery] string search = "")
        {
            try
            {
                var contacts = await _context.Contacts
                    .AsNoTracking()
                    .Where(c => c.PartnerId == partnerId && 
                               (string.IsNullOrEmpty(search) || c.LastName.Contains(search)))
                    .OrderBy(c => c.LastName)
                    .Select(c => new
                    {
                        id = c.ContactId,
                        text = c.LastName + (string.IsNullOrEmpty(c.FirstName) ? "" : " " + c.FirstName)
                    })
                    .Take(50)
                    .ToListAsync();

                _logger.LogInformation("Fetched {ContactCount} contacts for PartnerId: {PartnerId}", contacts.Count, partnerId);
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contacts for PartnerId: {PartnerId}", partnerId);
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "Failed to retrieve contacts" } } });
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<ContactDto>>> GetAll()
        {
            try
            {
                var contacts = await _service.GetAllAsync();
                _logger.LogInformation("Fetched {ContactCount} contacts", contacts.Count);
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contacts");
                return StatusCode(500, new { success = false, message = "Error retrieving contacts: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContactDto>> GetById(int id)
        {
            try
            {
                var contact = await _service.GetByIdAsync(id);
                if (contact == null)
                {
                    _logger.LogWarning("Contact not found for ContactId: {ContactId}", id);
                    return NotFound();
                }
                _logger.LogInformation("Fetched contact with ContactId: {ContactId}", id);
                return Ok(contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contact: {ContactId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving contact: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateContactDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for CreateContactDto");
                    return BadRequest(new { success = false, message = "Érvénytelen adatok." });
                }
                var contact = await _service.CreateAsync(dto);
                _logger.LogInformation("Created contact with ContactId: {ContactId}", contact.ContactId);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { success = true, message = "Kontakt létrehozva sikeresen!", data = contact });
                }
                return CreatedAtAction(nameof(GetById), new { id = contact.ContactId }, contact);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating contact");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? BadRequest(new { success = false, message = "Kontakt létrehozása sikertelen. Próbálja újra." })
                    : BadRequest("Kontakt létrehozása sikertelen.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateContactDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for UpdateContactDto");
                    return BadRequest(new { success = false, message = "Érvénytelen adatok." });
                }
                var updated = await _service.UpdateAsync(id, dto);
                if (updated == null)
                {
                    _logger.LogWarning("Contact not found for ContactId: {ContactId}", id);
                    return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                        ? NotFound(new { success = false, message = "Kontakt nem található!" })
                        : NotFound();
                }
                _logger.LogInformation("Updated contact with ContactId: {ContactId}", id);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { success = true, message = "Kontakt frissítve sikeresen!", data = updated });
                }
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating contact {Id}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact {Id}", id);
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? BadRequest(new { success = false, message = "Kontakt frissítése sikertelen. Próbálja újra." })
                    : BadRequest("Kontakt frissítése sikertelen.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!await _service.DeleteAsync(id))
                {
                    _logger.LogWarning("Contact not found for deletion, ContactId: {ContactId}", id);
                    return NotFound(new { success = false, message = "Kontakt nem található!" });
                }
                _logger.LogInformation("Deleted contact with ContactId: {ContactId}", id);
                return Ok(new { success = true, message = "Kontakt törölve sikeresen!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact {ContactId}", id);
                return BadRequest(new { success = false, message = "Kontakt törlése sikertelen. Próbálja újra." });
            }
        }
    }
}
