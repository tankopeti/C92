using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Cloud9_2.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;


namespace Cloud9_2.Controllers
{
    public class TaskAssigneeUpdateDto
    {
        public string? AssignedToId { get; set; }
    }

    public class AttachDocumentsRequest
    {
        public List<int> DocumentIds { get; set; } = new();
        public string? Note { get; set; } // opcionális
    }


    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ILogger<TasksController> _logger;
        private readonly TaskPMService _taskService;
        private readonly ApplicationDbContext _context;

        public TasksController(
            ILogger<TasksController> logger,
            TaskPMService taskService,
            ApplicationDbContext context)
        {
            _logger = logger;
            _taskService = taskService;
            _context = context;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
                                       ?? throw new UnauthorizedAccessException("User ID not found in token.");

        // -----------------------------------------------------------------
        // GET: api/tasks
        // -----------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<List<TaskPMDto>>> GetAllTasks()
        {
            try
            {
                var tasks = await _taskService.GetAllTasksAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all tasks");
                return StatusCode(500, "An error occurred while retrieving tasks.");
            }
        }

        // -----------------------------------------------------------------
        // GET: api/tasks/{id}
        // -----------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskPMDto>> GetTask(int id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                    return NotFound($"Task with ID {id} not found.");

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task {TaskId}", id);
                return StatusCode(500, "An error occurred while retrieving the task.");
            }
        }

        // -----------------------------------------------------------------
        // POST: api/tasks/{taskId}/documents/attach
        // Body: { documentIds: [136523, 137527], note: "..." }
        // -----------------------------------------------------------------
        [HttpPost("{taskId:int}/documents/attach")]
        public async Task<IActionResult> AttachDocumentsToTask(int taskId, [FromBody] AttachDocumentsRequest req)
        {
            try
            {
                var ids = (req?.DocumentIds ?? new List<int>())
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

                if (ids.Count == 0)
                    return BadRequest("No document ids provided.");

                await _taskService.AttachDocumentsAsync(taskId, ids, CurrentUserId, req?.Note);

                // opcionális: visszaadhatod a friss taskot is (frontendnek kényelmes)
                var updated = await _taskService.GetTaskByIdAsync(taskId);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AttachDocumentsToTask failed taskId={TaskId}", taskId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // -----------------------------------------------------------------
        // DELETE: api/tasks/{taskId}/documents/{documentLinkId}
        // ⚠️ documentLinkId = TaskDocumentLinks.Id (nem DocumentId)
        // -----------------------------------------------------------------
        [HttpDelete("{taskId:int}/documents/{documentLinkId:int}")]
        public async Task<IActionResult> RemoveDocumentFromTask(int taskId, int documentLinkId)
        {
            try
            {
                await _taskService.RemoveDocumentAsync(taskId, documentLinkId, CurrentUserId);

                // opcionális: visszaadhatod a friss taskot is
                var updated = await _taskService.GetTaskByIdAsync(taskId);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveDocumentFromTask failed taskId={TaskId} linkId={LinkId}", taskId, documentLinkId);
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // -----------------------------------------------------------------
        // POST: api/tasks
        // -----------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<TaskPMDto>> CreateTask([FromBody] TaskCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdTask = await _taskService.CreateTaskAsync(dto, CurrentUserId);
                return CreatedAtAction(nameof(GetTask), new { id = createdTask.Id }, createdTask);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error in CreateTask");
                return BadRequest(new { errors = new { General = new[] { ex.Message } } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateTask failed. User: {UserId}, DTO: {@DTO}", CurrentUserId, dto);
                return StatusCode(500, new { errors = new { General = new[] { "Szerver hiba. Kérjük, próbálja később." } } });
            }

        }


        // -----------------------------------------------------------------
        // PUT: api/tasks/{id}
        // -----------------------------------------------------------------
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskPMDto>> UpdateTask(int id, [FromBody] TaskUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Task ID in URL does not match payload.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updatedTask = await _taskService.UpdateTaskAsync(dto, CurrentUserId);
                return Ok(updatedTask);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Task with ID {id} not found.");
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", id);
                return StatusCode(500, "An error occurred while updating the task.");
            }
        }

        // -----------------------------------------------------------------
        // DELETE: api/tasks/{id}
        // -----------------------------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTaskAsync(int id)
        {
            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.TaskPM
            SET 
                IsActive = 0,
                UpdatedDate = {DateTime.UtcNow}
            WHERE 
                Id = {id}
                AND IsActive = 1;
            ");

            _logger.LogInformation(
                "DeleteTaskAsync (SQL) TaskPM id={Id} affectedRows={Affected}",
                id,
                affected
            );

            if (affected == 0)
                return NotFound($"Task {id} not found or already deleted.");

            return NoContent(); // 204
        }



        // -----------------------------------------------------------------
        // GET: api/tasks/paged
        // -----------------------------------------------------------------
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<TaskPMDto>>> GetPagedTasks(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sort = null,
            [FromQuery] string? order = "desc",           // figyeld: desc az alap!
            [FromQuery] int? statusId = null,
            [FromQuery] int? priorityId = null,
            [FromQuery] int? taskTypeId = null,           // ÚJ
            [FromQuery] int? partnerId = null,            // ÚJ
            [FromQuery] int? siteId = null,               // ÚJ
            [FromQuery] string? assignedToId = null,      // marad string
            [FromQuery] DateTime? dueDateFrom = null,     // ÚJ
            [FromQuery] DateTime? dueDateTo = null,       // ÚJ
            [FromQuery] DateTime? createdDateFrom = null, // ÚJ
            [FromQuery] DateTime? createdDateTo = null)   // ÚJ
        {
            try
            {
                var result = await _taskService.GetPagedTasksAsync(
                    page: page,
                    pageSize: pageSize,
                    searchTerm: search,
                    sort: sort,
                    order: order,
                    statusId: statusId,
                    priorityId: priorityId,
                    taskTypeId: taskTypeId,
                    partnerId: partnerId,
                    siteId: siteId,
                    assignedToId: assignedToId,
                    dueDateFrom: dueDateFrom,
                    dueDateTo: dueDateTo,
                    createdDateFrom: createdDateFrom,
                    createdDateTo: createdDateTo
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paged tasks");
                return StatusCode(500, "An error occurred while retrieving paged tasks.");
            }
        }

        [HttpGet("assignees/select")]
        public async Task<IActionResult> GetAssigneesForSelect()
        {
            try
            {
                var users = await _context.Users
                    .AsNoTracking()
                    .OrderBy(u => u.NormalizedUserName)
                    .Select(u => new
                    {
                        id = u.Id,
                        text = u.UserName + (u.Email != null ? $" ({u.Email})" : "")
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching assignees for select");
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // === ÚJ SELECT VÉGPONTOK ===

        [HttpGet("tasktypes/select")]
        public async Task<IActionResult> GetTaskTypes()
        {
            try
            {
                var taskTypes = await _context.TaskTypePMs
                    .Where(t => t.IsActive != false) // optional: only active
                    .Select(t => new { id = t.TaskTypePMId, text = t.TaskTypePMName })
                    .OrderBy(t => t.text)
                    .ToListAsync();

                return Ok(taskTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task types for select");
                return StatusCode(500, "An error occurred while retrieving task types.");
            }
        }

        // === KOMMUNIKÁCIÓS MÓD SELECT VÉGPONT ===
        [HttpGet("taskpm-communication-methods/select")]
        public async Task<IActionResult> GetTaskPmCommunicationMethods()
        {
            try
            {
                var methods = await _context.Set<TaskPMcomMethod>()
                    .AsNoTracking()
                    .Where(m => m.Aktiv == true)
                    .OrderBy(m => m.Sorrend ?? 0)
                    .ThenBy(m => m.Nev)
                    .Select(m => new
                    {
                        id = m.TaskPMcomMethodID,
                        text = m.Nev
                    })
                    .ToListAsync();

                return Ok(methods);
            }
            catch (Exception ex)
            {
                // EZ FONTOS: így a valódi hibát ki fogja írni (SQL hiba / mapping hiba / stb.)
                _logger.LogError(ex, "Error fetching TaskPM communication methods for select");
                return StatusCode(500, new { error = ex.Message });
            }
        }



        // -----------------------------------------------------------------
        // GET: api/tasks/taskstatuses/select
        // -----------------------------------------------------------------
        [HttpGet("taskstatuses/select")]
        public async Task<IActionResult> GetTaskStatuses()
        {
            try
            {
                var statuses = await _context.TaskStatusesPM
                        .Select(s => new { id = s.TaskStatusPMId, text = s.Name })
                        .OrderBy(s => s.text)
                        .ToListAsync();

                return Ok(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task statuses for select");
                return StatusCode(500, "An error occurred while retrieving task statuses.");
            }
        }


        [HttpPut("{id:int}/assignee/sql")]
        public async Task<IActionResult> UpdateAssigneeSql(int id, [FromBody] TaskAssigneeUpdateDto dto)
        {
            try
            {
                var affected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE dbo.TaskPM
SET
    AssignedTo = {dto.AssignedToId},
    UpdatedDate = {DateTime.UtcNow}
WHERE
    Id = {id}
    AND IsActive = 1;
");

                if (affected == 0)
                    return NotFound($"Task {id} not found or already deleted.");

                // visszaadjuk a frissített DTO-t, hogy a frontend csak a sort frissítse
                var updated = await _taskService.GetTaskByIdAsync(id);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateAssigneeSql failed for TaskId={TaskId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // -----------------------------------------------------------------
        // GET: api/tasks/taskpriorities/select
        // -----------------------------------------------------------------
        [HttpGet("taskpriorities/select")]
        public async Task<IActionResult> GetTaskPriorities()
        {
            try
            {
                var priorities = await _context.TaskPrioritiesPM
                    .Select(p => new { id = p.TaskPriorityPMId, text = p.Name })
                    .OrderBy(p => p.text)
                    .ToListAsync();

                return Ok(priorities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task priorities for select");
                return StatusCode(500, "An error occurred while retrieving task priorities.");
            }
        }

        [HttpGet("partners/select")]
        public async Task<IActionResult> SearchPartners([FromQuery] string? q = null)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Ok(new List<object>());

            var term = q.Trim();

            var result = await _context.Partners
            .Where(p => p.IsActive &&
            (EF.Functions.Like(p.Name, "%" + term + "%") ||
                EF.Functions.Like(p.TaxId, "%" + term + "%") ||
                EF.Functions.Like(p.CompanyName, "%" + term + "%")))

                .OrderBy(p => p.Name)
                .Take(100)
                .Select(p => new
                {
                    id = p.PartnerId,
                    text = string.IsNullOrEmpty(p.CompanyName)
                        ? p.Name
                        : $"{p.Name} • {p.CompanyName}"  // szép megjelenítés: "Tesco • Tesco Magyarország Zrt."
                })
                .ToListAsync();

            return Ok(result);
        }

        // -----------------------------------------------------------------
        // GET: api/tasks/documents/picker?q=...  (DOCUMENT PICKER LIST)
        // -----------------------------------------------------------------
        [HttpGet("documents/picker")]
        public async Task<IActionResult> GetDocumentsForPicker([FromQuery] string? q = null, [FromQuery] int take = 50)
        {
            take = Math.Clamp(take, 1, 200);

            var query = _context.Documents.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(d =>
                    EF.Functions.Like(d.FileName, "%" + term + "%")
                // ha van más meződ: cím, leírás, partner, stb.
                );
            }

            var items = await query
                .OrderByDescending(d => d.DocumentId) // vagy CreatedDate desc
                .Take(take)
                .Select(d => new
                {
                    id = d.DocumentId,
                    fileName = d.FileName,
                    filePath = d.FilePath
                })
                .ToListAsync();

            return Ok(items);
        }


    }


}