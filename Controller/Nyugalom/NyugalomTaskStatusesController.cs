using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
                    .Select(s => new
                    {
                        id = s.TaskStatusPMId,
                        text = s.Name,
                        color = s.ColorCode ?? "#6c757d"
                    })
                    .ToListAsync();

                return Ok(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nyugalom task statuses lekérdezési hiba");
                return StatusCode(500, "Hiba történt.");
            }
        }

        // POST: api/nyugalom/taskstatuses/change
// POST: api/nyugalom/taskstatuses/change
[HttpPost("change")]
public async Task<IActionResult> ChangeTaskStatus([FromBody] ChangeStatusRequest request)
{
    if (request == null || request.TaskId <= 0 || request.NewStatusId <= 0)
    {
        return BadRequest(new { success = false, message = "Érvénytelen adatok" });
    }

    try
    {
        // Ellenőrizzük, hogy a státusz létezik-e és Nyugalom státusz (TaskStatusPMId > 1000)
        var validStatus = await _context.TaskStatusesPM
            .AnyAsync(s => s.TaskStatusPMId == request.NewStatusId && s.TaskStatusPMId > 1000);

        if (!validStatus)
        {
            return BadRequest(new { success = false, message = "Érvénytelen státusz" });
        }

        // Ellenőrizzük, hogy a feladat létezik-e
        var task = await _context.TaskPMs.FirstOrDefaultAsync(t => t.Id == request.TaskId && t.IsActive);
        if (task == null)
        {
            return NotFound(new { success = false, message = "Feladat nem található" });
        }

        // Régi státusz ID a history-hoz
        var oldStatusId = task.TaskStatusPMId;

        // Közvetlen UPDATE SQL – csak egy rekord módosul
        await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE TaskPM 
              SET TaskStatusPMId = {0}, UpdatedDate = GETDATE()
              WHERE Id = {1}",
            request.NewStatusId, request.TaskId);

        // Manuális history hozzáadása, mivel raw SQL megkerülte az interceptort
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var changeDescription = $"Státusz megváltoztatva: {oldStatusId} -> {request.NewStatusId}";

        var history = new TaskHistory
        {
            TaskPMId = request.TaskId,
            ModifiedById = userId,
            ModifiedDate = DateTime.UtcNow,
            ChangeDescription = changeDescription
        };

        _context.TaskHistories.Add(history);
        await _context.SaveChangesAsync();  // Ez menti a history-t, és triggerelheti az interceptort is, ha kell

        // Új státusz adatok lekérdezése a válaszhoz
        var newStatus = await _context.TaskStatusesPM
            .Where(s => s.TaskStatusPMId == request.NewStatusId)
            .Select(s => new { s.Name, ColorCode = s.ColorCode ?? "#6c757d" })
            .FirstOrDefaultAsync();

        if (newStatus == null)
        {
            return NotFound(new { success = false, message = "Státusz nem található" });
        }

        _logger.LogInformation("Feladat {TaskId} státusza módosítva {NewStatusId}-re", request.TaskId, request.NewStatusId);

        return Ok(new
        {
            success = true,
            statusName = newStatus.Name,
            colorCode = newStatus.ColorCode
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Státusz módosítás hiba TaskId={TaskId}", request?.TaskId);
        return StatusCode(500, new { success = false, message = "Szerver hiba" });
    }
}
    }

    public class ChangeStatusRequest
    {
        public int TaskId { get; set; }
        public int NewStatusId { get; set; }
    }
}