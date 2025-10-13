using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentTermsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentTermsController> _logger;

        public PaymentTermsController(
            ApplicationDbContext context,
            ILogger<PaymentTermsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/paymentterms/select?search=abc
        [HttpGet("select")]
        public async Task<IActionResult> GetPaymentTermsForSelect([FromQuery] string search = "")
        {
            try
            {
                var paymentTerms = await _context.PaymentTerms
                    .AsNoTracking()
                    .Where(pt => string.IsNullOrEmpty(search) || pt.TermName.Contains(search))
                    .OrderBy(pt => pt.TermName)
                    .Select(pt => new
                    {
                        id = pt.PaymentTermId,
                        text = pt.TermName
                    })
                    .Take(50)
                    .ToListAsync();

                _logger.LogInformation("Fetched {PaymentTermCount} payment terms for select", paymentTerms.Count);
                return Ok(paymentTerms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment terms for select");
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "Failed to retrieve payment terms" } } });
            }
        }

        // GET: api/paymentterms
        [HttpGet]
        public async Task<ActionResult<List<PaymentTermDTO>>> GetAll()
        {
            try
            {
                var paymentTerms = await _context.PaymentTerms
                    .AsNoTracking()
                    .Select(pt => new PaymentTermDTO
                    {
                        PaymentTermId = pt.PaymentTermId,
                        TermName = pt.TermName,
                        Description = pt.Description,
                        DaysDue = pt.DaysDue,
                        CreatedBy = pt.CreatedBy,
                        CreatedDate = pt.CreatedDate,
                        ModifiedBy = pt.ModifiedBy,
                        ModifiedDate = pt.ModifiedDate
                    })
                    .ToListAsync();

                _logger.LogInformation("Fetched {PaymentTermCount} payment terms", paymentTerms.Count);
                return Ok(paymentTerms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment terms");
                return StatusCode(500, new { success = false, message = "Error retrieving payment terms: " + ex.Message });
            }
        }

        // GET: api/paymentterms/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentTermDTO>> GetById(int id)
        {
            try
            {
                var paymentTerm = await _context.PaymentTerms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pt => pt.PaymentTermId == id);

                if (paymentTerm == null)
                {
                    _logger.LogWarning("Payment term not found for PaymentTermId: {PaymentTermId}", id);
                    return NotFound(new { success = false, message = "Fizetési feltétel nem található!" });
                }

                var dto = new PaymentTermDTO
                {
                    PaymentTermId = paymentTerm.PaymentTermId,
                    TermName = paymentTerm.TermName,
                    Description = paymentTerm.Description,
                    DaysDue = paymentTerm.DaysDue,
                    CreatedBy = paymentTerm.CreatedBy,
                    CreatedDate = paymentTerm.CreatedDate,
                    ModifiedBy = paymentTerm.ModifiedBy,
                    ModifiedDate = paymentTerm.ModifiedDate
                };

                _logger.LogInformation("Fetched payment term with PaymentTermId: {PaymentTermId}", id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment term: {PaymentTermId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving payment term: " + ex.Message });
            }
        }

        // POST: api/paymentterms
        [HttpPost]
        public async Task<IActionResult> Create(PaymentTermCreateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for PaymentTermCreateDTO");
                    return BadRequest(new { success = false, message = "Érvénytelen adatok." });
                }

                var paymentTerm = new PaymentTerm
                {
                    TermName = dto.TermName,
                    Description = dto.Description,
                    DaysDue = dto.DaysDue,
                    CreatedBy = User.Identity?.Name ?? "System",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedBy = User.Identity?.Name ?? "System",
                    ModifiedDate = DateTime.UtcNow
                };

                _context.PaymentTerms.Add(paymentTerm);
                await _context.SaveChangesAsync();

                var resultDto = new PaymentTermDTO
                {
                    PaymentTermId = paymentTerm.PaymentTermId,
                    TermName = paymentTerm.TermName,
                    Description = paymentTerm.Description,
                    DaysDue = paymentTerm.DaysDue,
                    CreatedBy = paymentTerm.CreatedBy,
                    CreatedDate = paymentTerm.CreatedDate,
                    ModifiedBy = paymentTerm.ModifiedBy,
                    ModifiedDate = paymentTerm.ModifiedDate
                };

                _logger.LogInformation("Created payment term with PaymentTermId: {PaymentTermId}", paymentTerm.PaymentTermId);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { success = true, message = "Fizetési feltétel létrehozva sikeresen!", data = resultDto });
                }
                return CreatedAtAction(nameof(GetById), new { id = paymentTerm.PaymentTermId }, resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment term");
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? BadRequest(new { success = false, message = "Fizetési feltétel létrehozása sikertelen. Próbálja újra." })
                    : BadRequest("Fizetési feltétel létrehozása sikertelen.");
            }
        }

        // PUT: api/paymentterms/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, PaymentTermUpdateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid || id != dto.PaymentTermId)
                {
                    _logger.LogWarning("Invalid model state or ID mismatch for PaymentTermUpdateDTO, ID: {Id}", id);
                    return BadRequest(new { success = false, message = "Érvénytelen adatok vagy azonosító eltérés." });
                }

                var paymentTerm = await _context.PaymentTerms.FindAsync(id);
                if (paymentTerm == null)
                {
                    _logger.LogWarning("Payment term not found for PaymentTermId: {PaymentTermId}", id);
                    return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                        ? NotFound(new { success = false, message = "Fizetési feltétel nem található!" })
                        : NotFound();
                }

                paymentTerm.TermName = dto.TermName;
                paymentTerm.Description = dto.Description;
                paymentTerm.DaysDue = dto.DaysDue;
                paymentTerm.ModifiedBy = User.Identity?.Name ?? "System";
                paymentTerm.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var resultDto = new PaymentTermDTO
                {
                    PaymentTermId = paymentTerm.PaymentTermId,
                    TermName = paymentTerm.TermName,
                    Description = paymentTerm.Description,
                    DaysDue = paymentTerm.DaysDue,
                    CreatedBy = paymentTerm.CreatedBy,
                    CreatedDate = paymentTerm.CreatedDate,
                    ModifiedBy = paymentTerm.ModifiedBy,
                    ModifiedDate = paymentTerm.ModifiedDate
                };

                _logger.LogInformation("Updated payment term with PaymentTermId: {PaymentTermId}", id);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { success = true, message = "Fizetési feltétel frissítve sikeresen!", data = resultDto });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment term {Id}", id);
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? BadRequest(new { success = false, message = "Fizetési feltétel frissítése sikertelen. Próbálja újra." })
                    : BadRequest("Fizetési feltétel frissítése sikertelen.");
            }
        }

        // DELETE: api/paymentterms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var paymentTerm = await _context.PaymentTerms.FindAsync(id);
                if (paymentTerm == null)
                {
                    _logger.LogWarning("Payment term not found for deletion, PaymentTermId: {PaymentTermId}", id);
                    return NotFound(new { success = false, message = "Fizetési feltétel nem található!" });
                }

                _context.PaymentTerms.Remove(paymentTerm);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted payment term with PaymentTermId: {PaymentTermId}", id);
                return Ok(new { success = true, message = "Fizetési feltétel törölve sikeresen!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment term {PaymentTermId}", id);
                return BadRequest(new { success = false, message = "Fizetési feltétel törlése sikertelen. Próbálja újra." });
            }
        }
    }
}
