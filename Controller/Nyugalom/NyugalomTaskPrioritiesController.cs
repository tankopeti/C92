using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // <<< FONTOS! User ID lekéréséhez

namespace Cloud9_2.Controllers.Nyugalom
{
    [Route("api/nyugalom/taskpriorities")]
    [ApiController]
    [Authorize]
    public class NyugalomTaskPrioritiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NyugalomTaskPrioritiesController> _logger;

        public NyugalomTaskPrioritiesController(ApplicationDbContext context, ILogger<NyugalomTaskPrioritiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: api/nyugalom/taskpriorities/change
        [HttpPost("change")]
        public async Task<IActionResult> ChangeTaskPriority([FromBody] ChangePriorityRequest request)
        {
            _logger.LogInformation("Prioritás módosítás indítva - TaskId: {TaskId}, NewPriorityId: {NewPriorityId}", 
                request?.TaskId, request?.NewPriorityId);

            if (request == null || request.TaskId <= 0 || request.NewPriorityId <= 0)
            {
                _logger.LogWarning("Érvénytelen kérés a prioritás módosításban");
                return BadRequest(new { success = false, message = "Érvénytelen adatok" });
            }

            try
            {
                // 1. Prioritás létezés ellenőrzése
                var validPriority = await _context.TaskPrioritiesPM
                    .AnyAsync(p => p.TaskPriorityPMId == request.NewPriorityId);

                if (!validPriority)
                {
                    _logger.LogWarning("Érvénytelen prioritás ID: {NewPriorityId}", request.NewPriorityId);
                    return BadRequest(new { success = false, message = "Érvénytelen prioritás" });
                }

                // 2. Feladat létezés ellenőrzése + régi prioritás lekérdezése
                var task = await _context.TaskPMs
                    .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.IsActive);

                if (task == null)
                {
                    _logger.LogWarning("Feladat nem található vagy inaktív: {TaskId}", request.TaskId);
                    return NotFound(new { success = false, message = "Feladat nem található" });
                }

                var oldPriorityId = task.TaskPriorityPMId;

                // 3. Ha már ez a prioritás, ne csináljunk semmit
                if (oldPriorityId == request.NewPriorityId)
                {
                    var currentPriority = await _context.TaskPrioritiesPM
                        .Where(p => p.TaskPriorityPMId == request.NewPriorityId)
                        .Select(p => new
                        {
                            p.Name,
                            ColorCode = string.IsNullOrWhiteSpace(p.PriorityColorCode) ? "#ffc107" : p.PriorityColorCode.Trim()
                        })
                        .FirstAsync();

                    return Ok(new
                    {
                        success = true,
                        priorityName = currentPriority.Name,
                        colorCode = currentPriority.ColorCode
                    });
                }

                // 4. Régi és új prioritás nevének lekérdezése a history-hoz
                var oldPriorityName = await _context.TaskPrioritiesPM
                    .Where(p => p.TaskPriorityPMId == oldPriorityId)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync() ?? "Ismeretlen";

                var newPriority = await _context.TaskPrioritiesPM
                    .Where(p => p.TaskPriorityPMId == request.NewPriorityId)
                    .Select(p => new
                    {
                        p.Name,
                        ColorCode = string.IsNullOrWhiteSpace(p.PriorityColorCode) ? "#ffc107" : p.PriorityColorCode.Trim()
                    })
                    .FirstAsync();

                // 5. Raw SQL update – marad, mert nálad csak így működik jól
                _logger.LogInformation("Raw SQL prioritás módosítás: Task {TaskId} | {OldPriority} → {NewPriority}", 
                    request.TaskId, oldPriorityName, newPriority.Name);

                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE TaskPM SET TaskPriorityPMId = {0}, UpdatedDate = GETDATE() WHERE Id = {1}",
                    request.NewPriorityId, request.TaskId);

                // 6. Manuális history rekord (mert raw SQL megkerülte az interceptort)
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";

                var history = new TaskHistory
                {
                    TaskPMId = request.TaskId,
                    ModifiedById = currentUserId,
                    ModifiedDate = DateTime.UtcNow,
                    ChangeDescription = $"Prioritás: {oldPriorityName} → {newPriority.Name}"
                };

                _context.TaskHistories.Add(history);
                await _context.SaveChangesAsync(); // history mentése

                _logger.LogInformation("Prioritás módosítás sikeres (raw SQL + manuális history) - TaskId: {TaskId}, Új prioritás: {PriorityName}, User: {UserId}",
                    request.TaskId, newPriority.Name, currentUserId);

                // 7. Válasz a frontendnek
                return Ok(new
                {
                    success = true,
                    priorityName = newPriority.Name,
                    colorCode = newPriority.ColorCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prioritás módosítás hiba - TaskId: {TaskId}, NewPriorityId: {NewPriorityId}",
                    request?.TaskId, request?.NewPriorityId);
                return StatusCode(500, new { success = false, message = "Szerver hiba" });
            }
        }
    }

    public class ChangePriorityRequest
    {
        public int TaskId { get; set; }
        public int NewPriorityId { get; set; }
    }
}