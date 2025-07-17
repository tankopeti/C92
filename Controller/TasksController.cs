// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Cloud9_2.Models;
// using Cloud9_2.Services;
// using Cloud9_2.Data;
// using System.Text.Json;
// using Microsoft.EntityFrameworkCore;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Logging;

// namespace Cloud9_2.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     [Authorize]
//     public class TasksController : ControllerBase
//     {
//         private readonly ITaskService _taskService;
//         private readonly ILogger<TasksController> _logger;
//         private readonly ApplicationDbContext _context;
//         private readonly IUserService _userService;

//         public TasksController(
//             ApplicationDbContext context, 
//             ITaskService taskService, 
//             ILogger<TasksController> logger,
//             IUserService userService)
//         {
//             _context = context ?? throw new ArgumentNullException(nameof(context));
//             _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
//             _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//             _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            
//             _logger.LogInformation("TasksController instantiated, Context: {Context}, TaskService: {TaskService}", 
//                 _context != null ? "Not null" : "Null", 
//                 _taskService != null ? "Not null" : "Null");
//         }

//         [HttpGet]
//         [ProducesResponseType(StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         public async Task<IActionResult> GetAllTasks(bool includeInactive = false)
//         {
//             try
//             {
//                 _logger.LogInformation("Fetching all tasks (includeInactive: {IncludeInactive})", includeInactive);
//                 var tasks = await _taskService.GetAllTasksAsync(includeInactive);
//                 _logger.LogInformation("Retrieved {TaskCount} tasks", tasks.Count());
//                 return Ok(tasks);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error fetching tasks");
//                 return StatusCode(500, new { error = "Failed to retrieve tasks", details = ex.Message });
//             }
//         }

//         [HttpGet("{id}")]
//         [ProducesResponseType(StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status404NotFound)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         public async Task<IActionResult> GetTaskById(int id)
//         {
//             try
//             {
//                 _logger.LogInformation("Fetching task ID: {TaskId}", id);
//                 var task = await _taskService.GetTaskByIdAsync(id);
                
//                 if (task == null)
//                 {
//                     _logger.LogWarning("Task not found: {TaskId}", id);
//                     return NotFound(new { error = $"Task with ID {id} not found" });
//                 }

//                 return Ok(task);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error fetching task ID: {TaskId}", id);
//                 return StatusCode(500, new { error = "Failed to retrieve task", details = ex.Message });
//             }
//         }

//         [HttpPost]
//         [ProducesResponseType(StatusCodes.Status201Created)]
//         [ProducesResponseType(StatusCodes.Status400BadRequest)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         public async Task<IActionResult> CreateTask([FromBody] TaskPMCreateDto createDto)
//         {
//             _logger.LogInformation("CreateTask called with DTO: {@TaskPMCreateDto}", createDto);

//             if (createDto == null)
//             {
//                 _logger.LogWarning("CreateTask received null TaskPMCreateDto");
//                 return BadRequest(new { error = "Invalid task data: DTO is null" });
//             }

//             if (!ModelState.IsValid)
//             {
//                 var errors = ModelState
//                     .Where(x => x.Value.Errors.Count > 0)
//                     .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                
//                 _logger.LogWarning("Invalid task data submitted: {Errors}, TaskDTO: {@TaskDTO}", 
//                     JsonSerializer.Serialize(errors), createDto);
                
//                 return BadRequest(new { error = "Invalid task data", details = errors });
//             }

//             try
//             {
//                 // Validate foreign keys
//                 if (!await _context.TaskTypePMs.AnyAsync(t => t.Id == createDto.TaskTypePMId))
//                 {
//                     _logger.LogWarning("Invalid TaskTypePMId: {TaskTypePMId}", createDto.TaskTypePMId);
//                     return BadRequest(new { error = $"Invalid TaskTypePMId: {createDto.TaskTypePMId}" });
//                 }

//                 if (createDto.ProjectPMId.HasValue && 
//                     !await _context.ProjectPMs.AnyAsync(p => p.Id == createDto.ProjectPMId.Value))
//                 {
//                     _logger.LogWarning("Invalid ProjectPMId: {ProjectPMId}", createDto.ProjectPMId);
//                     return BadRequest(new { error = $"Invalid ProjectPMId: {createDto.ProjectPMId}" });
//                 }

//                 if (!await _context.TaskStatusPMs.AnyAsync(s => s.Id == createDto.TaskStatusPMId))
//                 {
//                     _logger.LogWarning("Invalid TaskStatusPMId: {TaskStatusPMId}", createDto.TaskStatusPMId);
//                     return BadRequest(new { error = $"Invalid TaskStatusPMId: {createDto.TaskStatusPMId}" });
//                 }

//                 if (!await _context.TaskPriorityPMs.AnyAsync(p => p.Id == createDto.TaskPriorityPMId))
//                 {
//                     _logger.LogWarning("Invalid TaskPriorityPMId: {TaskPriorityPMId}", createDto.TaskPriorityPMId);
//                     return BadRequest(new { error = $"Invalid TaskPriorityPMId: {createDto.TaskPriorityPMId}" });
//                 }

//                 if (!string.IsNullOrEmpty(createDto.AssignedToId) && 
//                     !await _context.Users.AnyAsync(u => u.Id == createDto.AssignedToId))
//                 {
//                     _logger.LogWarning("Invalid AssignedToId: {AssignedToId}", createDto.AssignedToId);
//                     return BadRequest(new { error = $"Invalid AssignedToId: {createDto.AssignedToId}" });
//                 }

//                 var userId = _userService.GetCurrentUserId();
//                 var task = await _taskService.CreateTaskAsync(createDto, userId);
                
//                 _logger.LogInformation("Created task with ID: {TaskId}", task.Id);
//                 return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
//             }
//             catch (ArgumentException ex)
//             {
//                 _logger.LogWarning(ex, "Validation error creating task");
//                 return BadRequest(new { error = ex.Message });
//             }
//             catch (DbUpdateException ex)
//             {
//                 _logger.LogError(ex, "Database error creating task");
//                 return StatusCode(500, new { 
//                     error = "Database error creating task", 
//                     details = ex.InnerException?.Message ?? ex.Message 
//                 });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Unexpected error creating task");
//                 return StatusCode(500, new { 
//                     error = "Failed to create task", 
//                     details = ex.Message, 
//                     stackTrace = ex.StackTrace 
//                 });
//             }
//         }

//         [HttpPut("{id}")]
//         [ProducesResponseType(StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status400BadRequest)]
//         [ProducesResponseType(StatusCodes.Status404NotFound)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskPMUpdateDto updateDto)
//         {
//             try
//             {
//                 _logger.LogInformation("UpdateTask called for TaskId: {TaskId}", id);
                
//                 var result = await _taskService.UpdateTaskAsync(id, updateDto);
                
//                 if (result == null)
//                 {
//                     _logger.LogWarning("Task not found: Task ID {TaskId}", id);
//                     return NotFound(new { error = $"Task with ID {id} not found" });
//                 }
                
//                 _logger.LogInformation("Updated task ID: {TaskId}", id);
//                 return Ok(result);
//             }
//             catch (DbUpdateException ex)
//             {
//                 _logger.LogError(ex, "Database error updating task ID: {TaskId}", id);
//                 return StatusCode(500, new { error = "Database error updating task" });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Unexpected error updating task ID: {TaskId}", id);
//                 return StatusCode(500, new { error = "Failed to update task" });
//             }
//         }

//         [HttpDelete("{id}")]
//         [ProducesResponseType(StatusCodes.Status204NoContent)]
//         [ProducesResponseType(StatusCodes.Status404NotFound)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         public async Task<IActionResult> DeleteTask(int id)
//         {
//             try
//             {
//                 _logger.LogInformation("Deleting task ID: {TaskId}", id);
                
//                 // Now we can properly await and check the result
//                 var success = await _taskService.DeleteTaskAsync(id);
                
//                 if (!success)
//                 {
//                     _logger.LogWarning("Task not found: {TaskId}", id);
//                     return NotFound(new { error = $"Task with ID {id} not found" });
//                 }
                
//                 _logger.LogInformation("Deleted task ID: {TaskId}", id);
//                 return NoContent();
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error deleting task ID: {TaskId}", id);
//                 return StatusCode(500, new { error = "Failed to delete task" });
//             }
//         }

//         [HttpGet("statuses")]
//         [ProducesResponseType(StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         public async Task<IActionResult> GetTaskStatuses()
//         {
//             try
//             {
//                 _logger.LogInformation("Fetching all task statuses");
//                 var statuses = await _taskService.GetTaskStatusesAsync();
//                 return Ok(statuses);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error fetching task statuses");
//                 return StatusCode(500, new { error = "Failed to retrieve task statuses" });
//             }
//         }

//         [HttpGet("priorities")]
//         [ProducesResponseType(StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         public async Task<IActionResult> GetTaskPriorities()
//         {
//             try
//             {
//                 _logger.LogInformation("Fetching all task priorities");
//                 var priorities = await _taskService.GetTaskPrioritiesAsync();
//                 return Ok(priorities);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error fetching task priorities");
//                 return StatusCode(500, new { error = "Failed to retrieve task priorities" });
//             }
//         }

//         [HttpGet("types")]
//         [ProducesResponseType(StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         public async Task<IActionResult> GetTaskTypes()
//         {
//             try
//             {
//                 _logger.LogInformation("Fetching all task types");
//                 var types = await _taskService.GetTaskTypesAsync();
//                 return Ok(types);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error fetching task types");
//                 return StatusCode(500, new { error = "Failed to retrieve task types" });
//             }
//         }

//         [HttpPost("{id}/complete")]
//         [ProducesResponseType(StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status404NotFound)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         public async Task<IActionResult> CompleteTask(int id)
//         {
//             try
//             {
//                 _logger.LogInformation("Completing task ID: {TaskId}", id);
//                 var task = await _taskService.GetTaskByIdAsync(id);
                
//                 if (task == null)
//                 {
//                     _logger.LogWarning("Task not found: {TaskId}", id);
//                     return NotFound(new { error = $"Task with ID {id} not found" });
//                 }

//                 var updateDto = new TaskPMUpdateDto
//                 {
//                     Title = task.Title,
//                     Description = task.Description,
//                     TaskTypePMId = task.TaskType.Id,
//                     ProjectPMId = task.Project?.Id,
//                     TaskStatusPMId = 3, // Assuming 3 is Completed status
//                     TaskPriorityPMId = task.Priority.Id,
//                     DueDate = task.DueDate,
//                     EstimatedHours = task.EstimatedHours,
//                     ActualHours = task.ActualHours,
//                     AssignedToId = task.AssignedTo,
//                     IsActive = task.IsActive
//                 };

//                 var result = await _taskService.UpdateTaskAsync(id, updateDto);
//                 return Ok(result);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error completing task ID: {TaskId}", id);
//                 return StatusCode(500, new { error = "Failed to complete task" });
//             }
//         }
//     }
// }