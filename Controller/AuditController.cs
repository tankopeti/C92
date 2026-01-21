using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cloud9_2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/audit?entityType=Document&entityId=123&take=200
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string entityType,
            [FromQuery] int entityId,
            [FromQuery] int take = 200)
        {
            if (string.IsNullOrWhiteSpace(entityType) || entityId <= 0)
                return BadRequest(new { error = "entityType és entityId kötelező." });

            take = Math.Clamp(take, 1, 500);

            var items = await _context.Set<AuditLog>()
                .AsNoTracking()
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.ChangedAt)
                .Take(take)
                .Select(a => new
                {
                    changedAt = a.ChangedAt,
                    changedByName = a.ChangedByName,
                    action = a.Action,
                    changes = a.Changes
                })
                .ToListAsync();

            return Ok(items);
        }
    }
}
