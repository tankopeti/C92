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
    public class PartnersController : ControllerBase
    {
        private readonly IPartnerService _partnerService;
        private readonly ILogger<PartnersController> _logger;
        private readonly ApplicationDbContext _context;

        public PartnersController(
            IPartnerService partnerService,
            ILogger<PartnersController> logger,
            ApplicationDbContext context)
        {
            _partnerService = partnerService ?? throw new ArgumentNullException(nameof(partnerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // GET: api/partners/select?search=abc – TomSelect keresőhöz
        [HttpGet("select")]
        public async Task<IActionResult> GetPartnersForSelect([FromQuery] string search = "")
        {
            try
            {
                const int MaxResults = 300;

                var query = _context.Partners
                    .AsNoTracking()
                    .Where(p => p.IsActive);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLowerInvariant();
                    query = query.Where(p =>
                        EF.Functions.Like((p.NameTrim ?? "").ToLower(), $"%{term}%") ||
                        EF.Functions.Like((p.CompanyNameTrim ?? "").ToLower(), $"%{term}%") ||
                        EF.Functions.Like((p.TaxIdTrim ?? ""), $"%{term}%") ||
                        EF.Functions.Like((p.City ?? "").ToLower(), $"%{term}%") ||
                        EF.Functions.Like((p.Email ?? "").ToLower(), $"%{term}%")
                    );
                }

                var partners = await query
                    .OrderByDescending(p => p.PartnerId)  // Legújabb felül!
                        // .ThenByDescending(p => p.PartnerId)     // Ha azonos dátum, akkor a nagyobb ID felül (azaz az újabb)                    .ThenBy(p => p.Name)
                    .Take(MaxResults)
                    .Select(p => new
                    {
                        id = p.PartnerId,
                        text = string.IsNullOrWhiteSpace(p.CompanyName)
                            ? p.Name ?? "Névtelen partner"
                            : $"{p.CompanyName} ({p.Name ?? "nincs magánnév"})",
                        partnerName = string.IsNullOrWhiteSpace(p.CompanyName) ? p.Name : p.CompanyName,
                        partnerDetails = $"{(string.IsNullOrWhiteSpace(p.CompanyName) ? p.Name : p.CompanyName)} " +
                                         $"{(string.IsNullOrWhiteSpace(p.City) ? "" : $"– {p.City}")}" +
                                         $"{(string.IsNullOrWhiteSpace(p.TaxId) ? "" : $" ({p.TaxId})")}".Trim()
                    })
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} partners for TomSelect, search='{Search}'", partners.Count, search);
                return Ok(partners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hiba a partnerek lekérdezésekor TomSelecthez");
                return StatusCode(500, new { message = "Szerver hiba a partnerek betöltésekor" });
            }
        }

// GET: api/partners – FŐ LISTÁZÓ ENDPOINT (keresés + összetett szűrés + pagination)
[HttpGet]
public async Task<ActionResult<IEnumerable<PartnerDto>>> GetPartners(
    [FromQuery] string? search = null,
    [FromQuery] string? name = null,
    [FromQuery] string? taxId = null,
    [FromQuery] int? statusId = null,
    [FromQuery] string? city = null,
    [FromQuery] string? postalCode = null,
    [FromQuery] string? emailDomain = null,
    [FromQuery] bool activeOnly = true,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50)
{
    try
    {
        // Validálás
        if (page < 1) page = 1;
        if (pageSize < 10) pageSize = 50;
        if (pageSize > 200) pageSize = 200; // max 200 rekord oldalanként

        var query = _context.Partners
            .AsNoTracking()
            .Include(p => p.Status)
            .Where(p => !activeOnly || p.IsActive);

        // Egyszerű keresőmező (felső kereső input)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                EF.Functions.Like((p.Name ?? "").ToLower(), $"%{term}%") ||
                EF.Functions.Like((p.CompanyName ?? "").ToLower(), $"%{term}%") ||
                EF.Functions.Like((p.Email ?? "").ToLower(), $"%{term}%") ||
                EF.Functions.Like((p.TaxId ?? ""), $"%{term}%") ||
                EF.Functions.Like((p.City ?? "").ToLower(), $"%{term}%"));
        }

        // Összetett szűrők a modalból
        if (!string.IsNullOrWhiteSpace(name))
        {
            var nameTerm = name.Trim().ToLowerInvariant();
            query = query.Where(p =>
                EF.Functions.Like((p.Name ?? "").ToLower(), $"%{nameTerm}%") ||
                EF.Functions.Like((p.CompanyName ?? "").ToLower(), $"%{nameTerm}%"));
        }

        if (!string.IsNullOrWhiteSpace(taxId))
        {
            query = query.Where(p => p.TaxId != null && EF.Functions.Like(p.TaxId, $"%{taxId.Trim()}%"));
        }

        if (statusId.HasValue)
        {
            query = query.Where(p => p.StatusId == statusId.Value);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityTerm = city.Trim().ToLowerInvariant();
            query = query.Where(p => p.City != null && EF.Functions.Like(p.City.ToLower(), $"%{cityTerm}%"));
        }

        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            query = query.Where(p => p.PostalCode == postalCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(emailDomain))
        {
            query = query.Where(p => p.Email != null && p.Email.EndsWith(emailDomain.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // Összes találat száma a headerhez
        var totalCount = await query.CountAsync();

        // Partnerek lekérdezése paginationnel és rendezéssel (legújabb felül)
        var partners = await query
            .OrderByDescending(p => p.PartnerId)  // legfontosabb: legújabb felül
            .ThenByDescending(p => p.CreatedDate)     // ha azonos dátum, akkor az újabb ID felül
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PartnerDto
            {
                PartnerId = p.PartnerId,
                Name = p.Name,
                CompanyName = p.CompanyName,
                Email = p.Email,
                PhoneNumber = p.PhoneNumber,
                TaxId = p.TaxId,
                AddressLine1 = p.AddressLine1,
                AddressLine2 = p.AddressLine2,
                City = p.City,
                State = p.State,
                PostalCode = p.PostalCode,
                StatusId = p.StatusId,
                Status = p.Status != null
                    ? new StatusDto
                    {
                        Id = p.Status.Id,
                        Name = p.Status.Name ?? "N/A",
                        Color = p.Status.Color ?? "#6c757d"
                    }
                    : null,
                PreferredCurrency = p.PreferredCurrency,
                AssignedTo = p.AssignedTo,
                IsActive = p.IsActive
            })
            .ToListAsync();

        // Total count elküldése a headerben (a JS Load More-hoz kell)
        Response.Headers.Add("X-Total-Count", totalCount.ToString());

        _logger.LogInformation("Fetched {Count} partners (page {Page}/{TotalPages}, total {TotalCount})", 
            partners.Count, page, (int)Math.Ceiling((double)totalCount / pageSize), totalCount);

        return Ok(partners);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Hiba a partnerek lekérdezésekor szűrőkkel és paginationnel");
        return StatusCode(500, new { message = "Hiba a partnerek betöltésekor" });
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
                    Sites = existing.Sites?.Select(s => new SiteDto { /* másolás */ }).ToList() ?? new List<SiteDto>(),
                    Contacts = existing.Contacts?.Select(c => new ContactDto { /* másolás */ }).ToList() ?? new List<ContactDto>(),
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

        // GET: api/partners/{id}/history
        [HttpGet("{id}/history")]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetPartnerHistory(int id)
        {
            var history = await _context.AuditLogs
                .Where(a => a.EntityType == "Partner" && a.EntityId == id)
                .OrderByDescending(a => a.ChangedAt)
                .Select(a => new AuditLogDto
                {
                    Action = a.Action,
                    ChangedByName = a.ChangedByName,
                    ChangedAt = a.ChangedAt,
                    Changes = a.Changes
                })
                .ToListAsync();

            return Ok(history);
        }
    }
}