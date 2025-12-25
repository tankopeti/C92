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
        public async Task<IActionResult> GetPartnersForSelect([FromQuery] string search = "")
        {
            try
            {
                const int MaxResults = 300; // Egységes a Sites és Nyugalom keresővel

                var query = _context.Partners
                    .AsNoTracking()
                    .Where(p => p.IsActive == true); // Csak aktív partnerek!

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();

                    query = query.Where(p =>
                        EF.Functions.Like(p.NameTrim ?? "", $"%{term}%") ||
                        EF.Functions.Like(p.CompanyNameTrim ?? "", $"%{term}%") ||
                        EF.Functions.Like(p.TaxIdTrim ?? "", $"%{term}%") ||
                        EF.Functions.Like(p.City ?? "", $"%{term}%") ||
                        EF.Functions.Like(p.Email ?? "", $"%{term}%")
                    );
                }

                var partners = await query
                    .OrderBy(p => p.CompanyName ?? p.Name)
                    .ThenBy(p => p.Name)
                    .Take(MaxResults)
                    .Select(p => new
                    {
                        id = p.PartnerId,
                        text = string.IsNullOrWhiteSpace(p.CompanyName)
                            ? p.Name ?? "Névtelen partner"
                            : $"{p.CompanyName} ({p.Name ?? "nincs magánnév"})",

                        // Extra infók a dropdownhoz és JS-hez
                        partnerName = string.IsNullOrWhiteSpace(p.CompanyName) ? p.Name : p.CompanyName,
                        partnerDetails = $"{(string.IsNullOrWhiteSpace(p.CompanyName) ? p.Name : p.CompanyName)} " +
                                         $"{(string.IsNullOrWhiteSpace(p.City) ? "" : $"– {p.City}")} " +
                                         $"{(string.IsNullOrWhiteSpace(p.TaxId) ? "" : $"({p.TaxId})")}".Trim()
                    })
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} partners for TomSelect search='{Search}'", partners.Count, search);
                return Ok(partners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hiba a partnerek lekérdezésekor TomSelecthez, search='{Search}'", search);
                return StatusCode(500, new { message = "Szerver hiba a partnerek betöltésekor" });
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

        // Új DTO létrehozása másolatként – minden scalar mező másolása
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
            Notes = existing.Notes + (string.IsNullOrEmpty(existing.Notes) ? "" : "\n\n") + 
                    $"--- Másolat a(z) {existing.PartnerId} azonosítójú partnerből ({DateTime.Now:yyyy-MM-dd HH:mm}) ---",
            AssignedTo = existing.AssignedTo,
            BillingContactName = existing.BillingContactName,
            BillingEmail = existing.BillingEmail,
            PaymentTerms = existing.PaymentTerms,
            CreditLimit = existing.CreditLimit,
            PreferredCurrency = existing.PreferredCurrency,
            IsTaxExempt = existing.IsTaxExempt,
            PartnerGroupId = existing.PartnerGroupId,

            // Sites másolása – új SiteDto objektumokkal (mély másolat!)
            Sites = existing.Sites?.Select(s => new SiteDto
            {
                SiteName = s.SiteName,
                AddressLine1 = s.AddressLine1,
                AddressLine2 = s.AddressLine2,
                City = s.City,
                State = s.State,
                PostalCode = s.PostalCode,
                Country = s.Country,
                IsPrimary = s.IsPrimary,
                ContactPerson1 = s.ContactPerson1,
                ContactPerson2 = s.ContactPerson2,
                ContactPerson3 = s.ContactPerson3,
                Comment1 = s.Comment1,
                Comment2 = s.Comment2
                // Ha van StatusId vagy más mező a SiteDto-ban, azt is másold
            }).ToList() ?? new List<SiteDto>(),

            // Contacts másolása – új ContactDto objektumokkal (mély másolat!)
            Contacts = existing.Contacts?.Select(c => new ContactDto
            {
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                PhoneNumber2 = c.PhoneNumber2,
                JobTitle = c.JobTitle,
                Comment = c.Comment,
                Comment2 = c.Comment2,
                IsPrimary = c.IsPrimary
                // StatusId, CreatedDate stb. általában nem másolandó új kontakt esetén
            }).ToList() ?? new List<ContactDto>(),

            // Dokumentumokat NEM másoljuk – új partnerhez új fájlokat kell feltölteni
            Documents = new List<DocumentDto>()
        };

        var createdPartner = await _partnerService.CreatePartnerAsync(newPartnerDto);

        _logger.LogInformation("Copied partner from {OriginalId} to new PartnerId: {NewId}", id, createdPartner.PartnerId);
        return CreatedAtAction(nameof(GetPartner), new { id = createdPartner.PartnerId }, createdPartner);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error copying PartnerId: {PartnerId}", id);
        return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "An unexpected error occurred during copying" } } });
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

        // GET: api/partners/statuses
[HttpGet("statuses")]
public async Task<IActionResult> GetStatuses()
{
    try
    {
        var statuses = await _context.PartnerStatuses
            .AsNoTracking()
            .Select(s => new { s.Id, s.Name })
            .OrderBy(s => s.Name)
            .ToListAsync();

        return Ok(statuses);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching statuses");
        return StatusCode(500, new { message = "Hiba a státuszok betöltésekor" });
    }
}


        // // POST: api/partners/OnPostCreatePartner
        // [HttpPost("OnPostCreatePartner")]
        // public async Task<IActionResult> OnPostCreatePartner([FromBody] PartnerDto partnerDto)
        // {
        //     if (!ModelState.IsValid)
        //     {
        //         _logger.LogWarning("Invalid model state for partnerDto: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        //         return BadRequest(new
        //         {
        //             title = "One or more validation errors occurred",
        //             errors = ModelState.ToDictionary(
        //                 kvp => kvp.Key,
        //                 kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray())
        //         });
        //     }

        //     try
        //     {
        //         var createdPartner = await _partnerService.CreatePartnerAsync(partnerDto);
        //         return new JsonResult(new { success = true, partnerId = createdPartner.PartnerId });
        //     }
        //     catch (ArgumentException ex)
        //     {
        //         _logger.LogWarning(ex, "Validation error in OnPostCreatePartner: {Message}", ex.Message);
        //         return BadRequest(new { title = "Validation error", errors = new { General = new[] { ex.Message } } });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error creating partner");
        //         return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "An unexpected error occurred" } } });
        //     }
        // }
    }
}