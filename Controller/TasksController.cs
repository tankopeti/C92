using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Cloud9_2.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Controllers
{
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                await _taskService.DeleteTaskAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Task with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {TaskId}", id);
                return StatusCode(500, "An error occurred while deleting the task.");
            }
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
                           (p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            p.TaxId.Contains(term, StringComparison.OrdinalIgnoreCase) ||     // adószám is jó keresési kulcs!
                            p.CompanyName.Contains(term, StringComparison.OrdinalIgnoreCase))) // cégnév is
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


    }
    
    
}