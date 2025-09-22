using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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
                    .Take(50)
                    .ToListAsync();

                _logger.LogInformation("Fetched {PartnerCount} partners for select", partners.Count);
                return Ok(partners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partners for select");
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "Failed to retrieve partners for select" } } });
            }
        }

        // GET: api/partners?searchTerm=&statusFilter=&sortBy=createddate&skip=0&take=50
        [HttpGet]
        public async Task<IActionResult> GetPartners(
            [FromQuery] string searchTerm = "",
            [FromQuery] string statusFilter = "",
            [FromQuery] string sortBy = "createddate",
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50)
        {
            try
            {
                var partners = await _partnerService.GetPartnersAsync(searchTerm, statusFilter, sortBy, skip, take);
                _logger.LogInformation("Fetched {PartnerCount} partners", partners.Count);
                return Ok(partners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partners");
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "Failed to retrieve partners" } } });
            }
        }

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
                    return NotFound(new { title = "Not found", errors = new { Id = new[] { $"Partner {id} not found" } } });
                }

                _logger.LogInformation("Fetched partner {PartnerId}: {PartnerName}", id, partner.Name);
                return Ok(partner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partner {PartnerId}", id);
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "Failed to retrieve partner" } } });
            }
        }

        // POST: api/partners/CreatePartner
        [HttpPost("CreatePartner")]
        public async Task<IActionResult> CreatePartner([FromBody] PartnerDto partnerDto)
        {
            if (partnerDto == null)
            {
                _logger.LogError("Received null partnerDto in CreatePartner");
                return BadRequest(new { title = "Invalid input", errors = new { General = new[] { "Partner data is null" } } });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for partnerDto: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new
                {
                    title = "One or more validation errors occurred",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }

            try
            {
                var createdPartner = await _partnerService.CreatePartnerAsync(partnerDto);
                return CreatedAtAction(nameof(GetPartner), new { id = createdPartner.PartnerId }, createdPartner);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error in CreatePartner: {Message}", ex.Message);
                return BadRequest(new { title = "Validation error", errors = new { General = new[] { ex.Message } } });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error in CreatePartner: {Message}", ex.Message);
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { ex.Message } } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreatePartner: {Message}", ex.Message);
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "An unexpected error occurred" } } });
            }
        }

        // PUT: api/partners/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePartner(int id, [FromBody] PartnerDto partnerDto)
        {
            _logger.LogInformation("UpdatePartner called for PartnerId: {PartnerId}", id);

            if (partnerDto == null)
            {
                _logger.LogWarning("UpdatePartner received null partnerDto for PartnerId: {PartnerId}", id);
                return BadRequest(new { title = "Invalid input", errors = new { General = new[] { "Partner data is required" } } });
            }

            if (id != partnerDto.PartnerId)
            {
                _logger.LogWarning("ID mismatch: URL ID={UrlId}, DTO ID={DtoId}", id, partnerDto.PartnerId);
                return BadRequest(new { title = "Invalid input", errors = new { Id = new[] { "ID mismatch" } } });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for PartnerId: {PartnerId}, Errors: {@Errors}", id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new
                {
                    title = "One or more validation errors occurred",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }

            try
            {
                var updatedPartner = await _partnerService.UpdatePartnerAsync(id, partnerDto);
                if (updatedPartner == null)
                {
                    _logger.LogWarning("Partner not found for PartnerId: {PartnerId}", id);
                    return NotFound(new { title = "Not found", errors = new { Id = new[] { $"Partner {id} not found" } } });
                }

                _logger.LogInformation("Updated partner with PartnerId: {PartnerId}, Name: {Name}", id, updatedPartner.Name);
                return Ok(updatedPartner);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating PartnerId: {PartnerId}: {Message}", id, ex.Message);
                return BadRequest(new { title = "Validation error", errors = new { General = new[] { ex.Message } } });
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict updating PartnerId: {PartnerId}", id);
                return Conflict(new { title = "Concurrency error", errors = new { General = new[] { "Partner was modified by another user" } } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating PartnerId: {PartnerId}", id);
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "An unexpected error occurred" } } });
            }
        }

        // POST: api/partners/{id}/copy
        [HttpPost("{id}/copy")]
        public async Task<IActionResult> CopyPartner(int id)
        {
            try
            {
                var existing = await _partnerService.GetPartnerAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Partner not found for copy, PartnerId: {PartnerId}", id);
                    return NotFound(new { title = "Not found", errors = new { Id = new[] { $"Partner {id} not found" } } });
                }

                var newPartnerDto = new PartnerDto
                {
                    Name = existing.Name + " (másolat)",
                    CompanyName = existing.CompanyName,
                    Email = existing.Email,
                    PhoneNumber = existing.PhoneNumber,
                    AlternatePhone = existing.AlternatePhone,
                    Website = existing.Website,
                    TaxId = existing.TaxId,
                    IntTaxId = existing.IntTaxId,
                    Industry = existing.Industry,
                    AddressLine1 = existing.AddressLine1,
                    AddressLine2 = existing.AddressLine2,
                    City = existing.City,
                    State = existing.State,
                    PostalCode = existing.PostalCode,
                    Country = existing.Country,
                    StatusId = existing.StatusId,
                    LastContacted = existing.LastContacted,
                    Notes = existing.Notes,
                    AssignedTo = existing.AssignedTo,
                    BillingContactName = existing.BillingContactName,
                    BillingEmail = existing.BillingEmail,
                    PaymentTerms = existing.PaymentTerms,
                    CreditLimit = existing.CreditLimit,
                    PreferredCurrency = existing.PreferredCurrency,
                    IsTaxExempt = existing.IsTaxExempt,
                    PartnerGroupId = existing.PartnerGroupId,
                    Sites = existing.Sites,
                    Contacts = existing.Contacts,
                    Documents = existing.Documents
                };

                var createdPartner = await _partnerService.CreatePartnerAsync(newPartnerDto);
                _logger.LogInformation("Copied partner from {OriginalId} to new PartnerId: {NewId}", id, createdPartner.PartnerId);
                return CreatedAtAction(nameof(GetPartner), new { id = createdPartner.PartnerId }, createdPartner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying PartnerId: {PartnerId}", id);
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "An unexpected error occurred" } } });
            }
        }


        // DELETE: api/partners/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePartner(int id)
        {
            try
            {
                var result = await _partnerService.DeletePartnerAsync(id);
                if (!result)
                {
                    _logger.LogWarning("Partner not found for deletion, PartnerId: {PartnerId}", id);
                    return NotFound(new { title = "Not found", errors = new { Id = new[] { $"Partner {id} not found" } } });
                }

                _logger.LogInformation("Deleted partner with PartnerId: {PartnerId}", id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete PartnerId: {PartnerId} due to related records", id);
                return BadRequest(new { title = "Validation error", errors = new { General = new[] { ex.Message } } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PartnerId: {PartnerId}", id);
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "An unexpected error occurred" } } });
            }
        }


        // POST: api/partners/OnPostCreatePartner
        [HttpPost("OnPostCreatePartner")]
        public async Task<IActionResult> OnPostCreatePartner([FromBody] PartnerDto partnerDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for partnerDto: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new
                {
                    title = "One or more validation errors occurred",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }

            try
            {
                var createdPartner = await _partnerService.CreatePartnerAsync(partnerDto);
                return new JsonResult(new { success = true, partnerId = createdPartner.PartnerId });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error in OnPostCreatePartner: {Message}", ex.Message);
                return BadRequest(new { title = "Validation error", errors = new { General = new[] { ex.Message } } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating partner");
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "An unexpected error occurred" } } });
            }
        }
    }
}