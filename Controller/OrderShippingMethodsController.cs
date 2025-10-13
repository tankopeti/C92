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
    public class OrderShippingMethodsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderShippingMethodsController> _logger;

        public OrderShippingMethodsController(
            ApplicationDbContext context,
            ILogger<OrderShippingMethodsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/ordershippingmethods/select?search=abc
        [HttpGet("select")]
        public async Task<IActionResult> GetShippingMethodsForSelect([FromQuery] string search = "")
        {
            try
            {
                var shippingMethods = await _context.OrderShippingMethods
                    .AsNoTracking()
                    .Where(sm => string.IsNullOrEmpty(search) || sm.MethodName.Contains(search))
                    .OrderBy(sm => sm.MethodName)
                    .Select(sm => new
                    {
                        id = sm.ShippingMethodId,
                        text = sm.MethodName
                    })
                    .Take(50)
                    .ToListAsync();

                _logger.LogInformation("Fetched {ShippingMethodCount} shipping methods for select", shippingMethods.Count);
                return Ok(shippingMethods);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shipping methods for select");
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "Failed to retrieve shipping methods" } } });
            }
        }

        // GET: api/ordershippingmethods
        [HttpGet]
        public async Task<ActionResult<List<OrderShippingMethodDTO>>> GetAll()
        {
            try
            {
                var shippingMethods = await _context.OrderShippingMethods
                    .AsNoTracking()
                    .Select(sm => new OrderShippingMethodDTO
                    {
                        ShippingMethodId = sm.ShippingMethodId,
                        MethodName = sm.MethodName,
                        Description = sm.Description,
                        CreatedBy = sm.CreatedBy,
                        CreatedDate = sm.CreatedDate,
                        ModifiedBy = sm.ModifiedBy,
                        ModifiedDate = sm.ModifiedDate
                    })
                    .ToListAsync();

                _logger.LogInformation("Fetched {ShippingMethodCount} shipping methods", shippingMethods.Count);
                return Ok(shippingMethods);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shipping methods");
                return StatusCode(500, new { success = false, message = "Error retrieving shipping methods: " + ex.Message });
            }
        }

        // GET: api/ordershippingmethods/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderShippingMethodDTO>> GetById(int id)
        {
            try
            {
                var shippingMethod = await _context.OrderShippingMethods
                    .AsNoTracking()
                    .FirstOrDefaultAsync(sm => sm.ShippingMethodId == id);

                if (shippingMethod == null)
                {
                    _logger.LogWarning("Shipping method not found for ShippingMethodId: {ShippingMethodId}", id);
                    return NotFound(new { success = false, message = "Szállítási mód nem található!" });
                }

                var dto = new OrderShippingMethodDTO
                {
                    ShippingMethodId = shippingMethod.ShippingMethodId,
                    MethodName = shippingMethod.MethodName,
                    Description = shippingMethod.Description,
                    CreatedBy = shippingMethod.CreatedBy,
                    CreatedDate = shippingMethod.CreatedDate,
                    ModifiedBy = shippingMethod.ModifiedBy,
                    ModifiedDate = shippingMethod.ModifiedDate
                };

                _logger.LogInformation("Fetched shipping method with ShippingMethodId: {ShippingMethodId}", id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shipping method: {ShippingMethodId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving shipping method: " + ex.Message });
            }
        }

        // POST: api/ordershippingmethods
        [HttpPost]
        public async Task<IActionResult> Create(OrderShippingMethodCreateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for OrderShippingMethodCreateDTO");
                    return BadRequest(new { success = false, message = "Érvénytelen adatok." });
                }

                var shippingMethod = new OrderShippingMethod
                {
                    MethodName = dto.MethodName,
                    Description = dto.Description,
                    CreatedBy = User.Identity?.Name ?? "System",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedBy = User.Identity?.Name ?? "System",
                    ModifiedDate = DateTime.UtcNow
                };

                _context.OrderShippingMethods.Add(shippingMethod);
                await _context.SaveChangesAsync();

                var resultDto = new OrderShippingMethodDTO
                {
                    ShippingMethodId = shippingMethod.ShippingMethodId,
                    MethodName = shippingMethod.MethodName,
                    Description = shippingMethod.Description,
                    CreatedBy = shippingMethod.CreatedBy,
                    CreatedDate = shippingMethod.CreatedDate,
                    ModifiedBy = shippingMethod.ModifiedBy,
                    ModifiedDate = shippingMethod.ModifiedDate
                };

                _logger.LogInformation("Created shipping method with ShippingMethodId: {ShippingMethodId}", shippingMethod.ShippingMethodId);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { success = true, message = "Szállítási mód létrehozva sikeresen!", data = resultDto });
                }
                return CreatedAtAction(nameof(GetById), new { id = shippingMethod.ShippingMethodId }, resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shipping method");
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? BadRequest(new { success = false, message = "Szállítási mód létrehozása sikertelen. Próbálja újra." })
                    : BadRequest("Szállítási mód létrehozása sikertelen.");
            }
        }

        // PUT: api/ordershippingmethods/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, OrderShippingMethodUpdateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid || id != dto.ShippingMethodId)
                {
                    _logger.LogWarning("Invalid model state or ID mismatch for OrderShippingMethodUpdateDTO, ID: {Id}", id);
                    return BadRequest(new { success = false, message = "Érvénytelen adatok vagy azonosító eltérés." });
                }

                var shippingMethod = await _context.OrderShippingMethods.FindAsync(id);
                if (shippingMethod == null)
                {
                    _logger.LogWarning("Shipping method not found for ShippingMethodId: {ShippingMethodId}", id);
                    return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                        ? NotFound(new { success = false, message = "Szállítási mód nem található!" })
                        : NotFound();
                }

                shippingMethod.MethodName = dto.MethodName;
                shippingMethod.Description = dto.Description;
                shippingMethod.ModifiedBy = User.Identity?.Name ?? "System";
                shippingMethod.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var resultDto = new OrderShippingMethodDTO
                {
                    ShippingMethodId = shippingMethod.ShippingMethodId,
                    MethodName = shippingMethod.MethodName,
                    Description = shippingMethod.Description,
                    CreatedBy = shippingMethod.CreatedBy,
                    CreatedDate = shippingMethod.CreatedDate,
                    ModifiedBy = shippingMethod.ModifiedBy,
                    ModifiedDate = shippingMethod.ModifiedDate
                };

                _logger.LogInformation("Updated shipping method with ShippingMethodId: {ShippingMethodId}", id);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { success = true, message = "Szállítási mód frissítve sikeresen!", data = resultDto });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shipping method {Id}", id);
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? BadRequest(new { success = false, message = "Szállítási mód frissítése sikertelen. Próbálja újra." })
                    : BadRequest("Szállítási mód frissítése sikertelen.");
            }
        }

        // DELETE: api/ordershippingmethods/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var shippingMethod = await _context.OrderShippingMethods.FindAsync(id);
                if (shippingMethod == null)
                {
                    _logger.LogWarning("Shipping method not found for deletion, ShippingMethodId: {ShippingMethodId}", id);
                    return NotFound(new { success = false, message = "Szállítási mód nem található!" });
                }

                _context.OrderShippingMethods.Remove(shippingMethod);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted shipping method with ShippingMethodId: {ShippingMethodId}", id);
                return Ok(new { success = true, message = "Szállítási mód törölve sikeresen!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shipping method {ShippingMethodId}", id);
                return BadRequest(new { success = false, message = "Szállítási mód törlése sikertelen. Próbálja újra." });
            }
        }
    }
}