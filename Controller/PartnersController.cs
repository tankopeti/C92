using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.IO;

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

            // ÚJ szűrők
            [FromQuery] int? gfoId = null,
            [FromQuery] int? partnerTypeId = null,
            [FromQuery] string? partnerCode = null,
            [FromQuery] string? ownId = null,

            [FromQuery] string? city = null,
            [FromQuery] string? postalCode = null,
            [FromQuery] string? emailDomain = null,

            [FromQuery] bool activeOnly = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var (items, total) = await _service.GetPartnersAsync(
                search: search,
                name: name,
                taxId: taxId,
                statusId: statusId,

                gfoId: gfoId,
                partnerTypeId: partnerTypeId,
                partnerCode: partnerCode,
                ownId: ownId,

                city: city,
                postalCode: postalCode,
                emailDomain: emailDomain,

                activeOnly: activeOnly,
                page: page,
                pageSize: pageSize
            );

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

        [HttpGet("export")]
        public async Task<IActionResult> ExportPartners(
    [FromQuery] string? search = null,
    [FromQuery] string? name = null,
    [FromQuery] string? taxId = null,
    [FromQuery] int? statusId = null,
    [FromQuery] int? gfoId = null,
    [FromQuery] int? partnerTypeId = null,
    [FromQuery] string? partnerCode = null,
    [FromQuery] string? ownId = null,
    [FromQuery] string? city = null,
    [FromQuery] string? postalCode = null,
    [FromQuery] bool activeOnly = true
)
        {
            // ⚠️ pageSize = nagy szám, paging nélkül
            var (items, _) = await _service.GetPartnersAsync(
                search: search,
                name: name,
                taxId: taxId,
                statusId: statusId,
                gfoId: gfoId,
                partnerTypeId: partnerTypeId,
                partnerCode: partnerCode,
                ownId: ownId,
                city: city,
                postalCode: postalCode,
                emailDomain: null, // vagy vedd fel queryből és add át
                activeOnly: activeOnly,
                page: 1,
                pageSize: 100_000
            );


            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Partnerek");

            int col = 1;
            int row = 1;

            // =========================
            // HEADER
            // =========================
            ws.Cell(row, col++).Value = "PartnerId";
            ws.Cell(row, col++).Value = "Név";
            ws.Cell(row, col++).Value = "Cégnév";
            ws.Cell(row, col++).Value = "Rövid név";
            ws.Cell(row, col++).Value = "Partner kód";
            ws.Cell(row, col++).Value = "Saját azonosító";
            ws.Cell(row, col++).Value = "GFO";
            ws.Cell(row, col++).Value = "Partner típus";
            ws.Cell(row, col++).Value = "Adószám";
            ws.Cell(row, col++).Value = "EU adószám";
            ws.Cell(row, col++).Value = "Magán adóazonosító";
            ws.Cell(row, col++).Value = "Iparág";
            ws.Cell(row, col++).Value = "Email";
            ws.Cell(row, col++).Value = "Telefon";
            ws.Cell(row, col++).Value = "Alternatív telefon";
            ws.Cell(row, col++).Value = "Weboldal";
            ws.Cell(row, col++).Value = "Utca";
            ws.Cell(row, col++).Value = "Utca 2";
            ws.Cell(row, col++).Value = "Város";
            ws.Cell(row, col++).Value = "Megye";
            ws.Cell(row, col++).Value = "Irányítószám";
            ws.Cell(row, col++).Value = "Ország";
            ws.Cell(row, col++).Value = "Státusz";
            ws.Cell(row, col++).Value = "Felelős";
            ws.Cell(row, col++).Value = "Utolsó kapcsolat";
            ws.Cell(row, col++).Value = "Aktív";
            ws.Cell(row, col++).Value = "Megjegyzés";
            ws.Cell(row, col++).Value = "Komment 1";
            ws.Cell(row, col++).Value = "Komment 2";
            ws.Cell(row, col++).Value = "Fizetési feltétel";
            ws.Cell(row, col++).Value = "Kredit limit";
            ws.Cell(row, col++).Value = "Pénznem";
            ws.Cell(row, col++).Value = "Számlázási név";
            ws.Cell(row, col++).Value = "Számlázási email";

            ws.Row(row).Style.Font.Bold = true;

            // =========================
            // DATA
            // =========================
            foreach (var p in items)
            {
                row++;
                col = 1;

                ws.Cell(row, col++).Value = p.PartnerId;
                ws.Cell(row, col++).Value = p.Name;
                ws.Cell(row, col++).Value = p.CompanyName;
                ws.Cell(row, col++).Value = p.ShortName;
                ws.Cell(row, col++).Value = p.PartnerCode;
                ws.Cell(row, col++).Value = p.OwnId;
                ws.Cell(row, col++).Value = p.GFOName;
                ws.Cell(row, col++).Value = p.PartnerTypeId;
                ws.Cell(row, col++).Value = p.TaxId;
                ws.Cell(row, col++).Value = p.IntTaxId;
                ws.Cell(row, col++).Value = p.IndividualTaxId;
                ws.Cell(row, col++).Value = p.Industry;
                ws.Cell(row, col++).Value = p.Email;
                ws.Cell(row, col++).Value = p.PhoneNumber;
                ws.Cell(row, col++).Value = p.AlternatePhone;
                ws.Cell(row, col++).Value = p.Website;
                ws.Cell(row, col++).Value = p.AddressLine1;
                ws.Cell(row, col++).Value = p.AddressLine2;
                ws.Cell(row, col++).Value = p.City;
                ws.Cell(row, col++).Value = p.State;
                ws.Cell(row, col++).Value = p.PostalCode;
                ws.Cell(row, col++).Value = p.Country;
                ws.Cell(row, col++).Value = p.Status?.Name;
                ws.Cell(row, col++).Value = p.AssignedTo;
                ws.Cell(row, col++).Value = p.LastContacted;
                ws.Cell(row, col++).Value = p.IsActive;
                ws.Cell(row, col++).Value = p.Notes;
                ws.Cell(row, col++).Value = p.Comment1;
                ws.Cell(row, col++).Value = p.Comment2;
                ws.Cell(row, col++).Value = p.PaymentTerms;
                ws.Cell(row, col++).Value = p.CreditLimit;
                ws.Cell(row, col++).Value = p.PreferredCurrency;
                ws.Cell(row, col++).Value = p.BillingContactName;
                ws.Cell(row, col++).Value = p.BillingEmail;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"partnerek_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}
