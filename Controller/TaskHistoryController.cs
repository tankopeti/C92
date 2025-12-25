using Cloud9_2.Models;
using Cloud9_2.Data;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cloud9_2.Controllers
{
[Route("api/tasks/{taskId}/history")]
[ApiController]
[Authorize]
public class TaskHistoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TaskHistoryController> _logger;

    public TaskHistoryController(ApplicationDbContext context, ILogger<TaskHistoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<ActionResult<List<TaskHistoryDto>>> GetTaskHistory(int taskId)
    {
        var histories = await _context.TaskHistories
            .AsNoTracking()
            .Where(h => h.TaskPMId == taskId)
            .Include(h => h.ModifiedBy)
            .OrderByDescending(h => h.ModifiedDate)
            .Select(h => new TaskHistoryDto
            {
                TaskHistoryId = h.TaskHistoryId,
                TaskPMId = h.TaskPMId,
                ModifiedById = h.ModifiedById,
                ModifiedByName = h.ModifiedBy != null ? h.ModifiedBy.UserName : "Ismeretlen",
                ModifiedDate = h.ModifiedDate,
                ChangeDescription = h.ChangeDescription
            })
            .ToListAsync();

        return Ok(histories);
    }
}

    }
