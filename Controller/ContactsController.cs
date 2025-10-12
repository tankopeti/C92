using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContactController : ControllerBase
    {
        private readonly ContactService _service;

        public ContactController(ContactService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<ContactDto>>> GetAll()
        {
            try
            {
                var contacts = await _service.GetAllAsync();
                return Ok(contacts);
            }
            catch (Exception ex)
            {
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
                    return NotFound();
                }
                return Ok(contact);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error retrieving contact: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateContactDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Érvénytelen adatok." });
                var contact = await _service.CreateAsync(dto);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { success = true, message = "Kontakt létrehozva sikeresen!", data = contact });
                }
                return CreatedAtAction(nameof(GetById), new { id = contact.ContactId }, contact);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // _service._logger.LogError(ex, "Error creating contact");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { success = false, message = "Kontakt létrehozása sikertelen. Próbálja újra." });
                }
                return BadRequest("Kontakt létrehozása sikertelen.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateContactDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Érvénytelen adatok." });
                var updated = await _service.UpdateAsync(id, dto);
                if (updated == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return NotFound(new { success = false, message = "Kontakt nem található!" });
                    }
                    return NotFound();
                }
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { success = true, message = "Kontakt frissítve sikeresen!", data = updated });
                }
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // _service._logger.LogError(ex, "Error updating contact {Id}", id);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { success = false, message = "Kontakt frissítése sikertelen. Próbálja újra." });
                }
                return BadRequest("Kontakt frissítése sikertelen.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!await _service.DeleteAsync(id))
                {
                    return NotFound(new { success = false, message = "Kontakt nem található!" });
                }
                return Ok(new { success = true, message = "Kontakt törölve sikeresen!" });
            }
            catch (Exception)
            {
                return BadRequest(new { success = false, message = "Kontakt törlése sikertelen. Próbálja újra." });
            }
        }
    }
}