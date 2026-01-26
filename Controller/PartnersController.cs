using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Data;
using Microsoft.EntityFrameworkCore;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PartnersController : ControllerBase
    {
        private readonly PartnerService _service;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PartnersController> _logger;

        public PartnersController(PartnerService service, ILogger<PartnersController> logger, ApplicationDbContext context)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // GET: api/partners – FŐ LISTÁZÓ ENDPOINT
        [HttpGet]
        public async Task<IActionResult> GetPartners(
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
            var (items, total) = await _service.GetPartnersAsync(
                search, name, taxId, statusId, city, postalCode, emailDomain,
                activeOnly, page, pageSize);

            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(items);
        }

        // GET: api/partners/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPartner(int id)
        {
            var dto = await _service.GetPartnerAsync(id);
            if (dto == null) return NotFound(new { message = $"Partner {id} not found" });
            return Ok(dto);
        }

        // POST: api/partners
        [HttpPost]
        public async Task<IActionResult> CreatePartner([FromBody] PartnerDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var created = await _service.CreatePartnerAsync(dto);
            return CreatedAtAction(nameof(GetPartner), new { id = created.PartnerId }, created);
        }

        // PUT: api/partners/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePartner(int id, [FromBody] PartnerDto dto)
        {
            if (dto == null) return BadRequest();
            if (id != dto.PartnerId) return BadRequest(new { message = "ID mismatch" });
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var updated = await _service.UpdatePartnerAsync(id, dto);
            if (updated == null) return NotFound(new { message = $"Partner {id} not found" });

            return Ok(updated);
        }

        // POST: api/partners/{id}/copy
        [HttpPost("{id:int}/copy")]
        public async Task<IActionResult> CopyPartner(int id)
        {
            var created = await _service.CopyPartnerAsync(id);
            if (created == null) return NotFound(new { message = $"Partner {id} not found" });

            return CreatedAtAction(nameof(GetPartner), new { id = created.PartnerId }, created);
        }

        // DELETE: api/partners/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePartner(int id)
        {
            var ok = await _service.DeletePartnerAsync(id);
            if (!ok) return NotFound(new { message = $"Partner {id} not found" });
            return NoContent();
        }

        // GET: api/partners/select?search=abc
        [HttpGet("select")]
        public async Task<IActionResult> GetPartnersForSelect([FromQuery] string search = "")
        {
            var items = await _service.GetPartnersForSelectAsync(search);
            return Ok(items);
        }

        // GET: api/partners/statuses
        [HttpGet("statuses")]
        public async Task<IActionResult> GetStatuses()
        {
            var statuses = await _service.GetStatusesAsync();
            return Ok(statuses);
        }

        // GET: api/partners/{id}/history
        [HttpGet("{id:int}/history")]
        public async Task<IActionResult> GetPartnerHistory(int id)
        {
            var history = await _service.GetPartnerHistoryAsync(id);
            return Ok(history);
        }

        [HttpGet("gfos")]
        public async Task<IActionResult> GetGfos()
        {
            var gfos = await _context.GFOs
                .AsNoTracking()
                .OrderBy(g => g.GFOName)
                .Select(g => new
                {
                    id = g.GFOId,
                    name = g.GFOName
                })
                .ToListAsync();

            return Ok(gfos);
        }

        [HttpGet("partnerTypes")]
        public async Task<IActionResult> GetPartnerTypes()
        {
            var partnerTypes = await _context.PartnerTypes
                .AsNoTracking()
                .OrderBy(pt => pt.PartnerTypeName)
                .Select(pt => new
                {
                    id = pt.PartnerTypeId,
                    name = pt.PartnerTypeName
                })
                .ToListAsync();

            return Ok(partnerTypes);
        }

    }
}
