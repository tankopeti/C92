using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using System.Data;
using Cloud9_2.Data;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderStatusTypesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrderStatusTypesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/OrderStatusTypes
        [HttpGet("select")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrderStatusTypesForSelect()
        {
            var statuses = await _context.OrderStatusTypes
                .Select(s => new
                {
                    id = s.OrderStatusId,
                    text = s.StatusName
                })
                .ToListAsync();

            return Ok(statuses);
        }


        // GET: api/OrderStatusTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderStatusType>> GetOrderStatusType(int id)
        {
            var orderStatusType = await _context.OrderStatusTypes.FindAsync(id);

            if (orderStatusType == null)
            {
                return NotFound();
            }

            return Ok(orderStatusType);
        }

        // POST: api/OrderStatusTypes
        [HttpPost]
        public async Task<ActionResult<OrderStatusType>> CreateOrderStatusType(OrderStatusType orderStatusType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.OrderStatusTypes.Add(orderStatusType);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrderStatusType), new { id = orderStatusType.OrderStatusId }, orderStatusType);
        }

        // PUT: api/OrderStatusTypes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderStatusType(int id, OrderStatusType orderStatusType)
        {
            if (id != orderStatusType.OrderStatusId)
            {
                return BadRequest();
            }

            _context.Entry(orderStatusType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderStatusTypeExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/OrderStatusTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderStatusType(int id)
        {
            var orderStatusType = await _context.OrderStatusTypes.FindAsync(id);
            if (orderStatusType == null)
            {
                return NotFound();
            }

            _context.OrderStatusTypes.Remove(orderStatusType);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderStatusTypeExists(int id)
        {
            return _context.OrderStatusTypes.Any(e => e.OrderStatusId == id);
        }
    }
}