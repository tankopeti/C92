using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Cloud9_2.Data;

namespace Cloud9_2.Controllers.Nyugalom
{
    [Route("api/nyugalom/taskstatuses")]
    [ApiController]
    [Authorize]
    public class NyugalomTaskStatusesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NyugalomTaskStatusesController> _logger;

        public NyugalomTaskStatusesController(ApplicationDbContext context, ILogger<NyugalomTaskStatusesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/nyugalom/taskstatuses/nyugalombejelentes
        [HttpGet("nyugalombejelentes")]
        public async Task<IActionResult> GetNyugalomStatuses()
        {
            try
            {
                var statuses = await _context.TaskStatusesPM
                    .Where(s => s.TaskStatusPMId > 1000)
                    .OrderBy(s => s.Name)
                    .Select(s => new { id = s.TaskStatusPMId, text = s.Name })
                    .ToListAsync();

                return Ok(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nyugalom task statuses error");
                return StatusCode(500, "Hiba történt.");
            }
        }

        // Ha később még több speciális státusz-végpont kell, ide teszed
    }
}