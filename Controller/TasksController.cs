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
        // GET: api/tasks/calendar/scheduled
        // FullCalendar automatikusan küld: ?start=...&end=...
        // -----------------------------------------------------------------
        [HttpGet("calendar/scheduled")]
        public async Task<IActionResult> GetScheduledCalendarEvents(
            [FromQuery] DateTime? start = null,
            [FromQuery] DateTime? end = null)
        {
            try
            {
                var from = start ?? DateTime.UtcNow.AddMonths(-1);
                var to = end ?? DateTime.UtcNow.AddMonths(2);

                var items = await _context.TaskPMs
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .Where(t => t.ScheduledDate != null)
                    .Where(t => t.ScheduledDate >= from && t.ScheduledDate < to)
                    .OrderBy(t => t.ScheduledDate)
                    .Select(t => new CalendarEventDto
                    {
                        id = t.Id.ToString(),
                        title = t.Title,

                        start = t.ScheduledDate!.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = t.ScheduledDate!.Value.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"), // ✅ legyen “fogható” blokk

                        allDay = false, // ✅ EZ A LÉNYEG

                        url = null,
                        color = null
                    })
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching scheduled calendar events");
                return StatusCode(500, new { error = "An error occurred while retrieving calendar events." });
            }
        }


        public class ScheduledDateUpdateDto
        {
            public DateTime ScheduledDate { get; set; }
        }

        [HttpPut("{id:int}/scheduled-date")]
        public async Task<IActionResult> UpdateScheduledDate(
            int id,
            [FromBody] ScheduledDateUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Missing body.");

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE dbo.TaskPM
SET
    ScheduledDate = {dto.ScheduledDate},
    UpdatedDate = {DateTime.UtcNow}
WHERE
    Id = {id}
    AND IsActive = 1;
");

            if (affected == 0)
                return NotFound($"Task {id} not found or already deleted.");

            return NoContent();
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
        // -----------------------------------------------------------------
        // GET: api/tasks/paged
        // -----------------------------------------------------------------
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<TaskPMDto>>> GetPagedTasks(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sort = null,
            [FromQuery] string? order = "desc",
            [FromQuery] int? statusId = null,
            [FromQuery] int? priorityId = null,
            [FromQuery] int? taskTypeId = null,
            [FromQuery] int? partnerId = null,
            [FromQuery] int? siteId = null,
            [FromQuery] string? assignedToId = null,
            [FromQuery] DateTime? dueDateFrom = null,
            [FromQuery] DateTime? dueDateTo = null,
            [FromQuery] DateTime? createdDateFrom = null,
            [FromQuery] DateTime? createdDateTo = null,

            // ✅ ÚJ: enum alapú szűrés (1=Bejelentes, 2=Intezkedes)
            [FromQuery] TaskDisplayType? displayType = null
        )
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
                    createdDateTo: createdDateTo,

                    // ✅ új param átadása service felé
                    displayType: displayType
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
        // GET: api/tasks/{taskId}/attachments
        // ✅ KELL A MODAL LISTÁHOZ
        // -----------------------------------------------------------------
        [HttpGet("{taskId:int}/attachments")]
        public async Task<IActionResult> GetAttachments(int taskId)
        {
            var taskExists = await _context.TaskPMs.AnyAsync(t => t.Id == taskId && t.IsActive);
            if (!taskExists) return NotFound($"Task {taskId} not found.");

            var items = await _context.TaskDocumentLinks
                .AsNoTracking()
                .Where(x => x.TaskId == taskId)
                .Include(x => x.Document)
                .Include(x => x.LinkedBy)
                .OrderByDescending(x => x.LinkedDate)
                .Select(x => new TaskDocumentDto
                {
                    Id = x.Id,
                    DocumentId = x.DocumentId,
                    FileName = x.Document != null ? (x.Document.FileName ?? "") : "",
                    FilePath = x.Document != null ? (x.Document.FilePath ?? "") : "",
                    LinkedDate = x.LinkedDate,
                    LinkedByName = x.LinkedBy != null ? x.LinkedBy.UserName : null,
                    Note = x.Note
                })
                .ToListAsync();

            return Ok(items);
        }

        // -----------------------------------------------------------------
        // POST: api/tasks/{taskId}/attachments/link
        // ✅ CSAK LINKET CSATOL
        // -----------------------------------------------------------------
        public class AttachLinkRequest
        {
            public string? FileName { get; set; } // opcionális
            public string Url { get; set; } = ""; // KÖTELEZŐ (link)
            public string? Note { get; set; }
        }

        [HttpPost("{taskId:int}/attachments/link")]
        public async Task<IActionResult> AttachLink(int taskId, [FromBody] AttachLinkRequest req)
        {
            if (req == null) return BadRequest("Missing body.");
            if (string.IsNullOrWhiteSpace(req.Url)) return BadRequest("Url is required.");

            var taskExists = await _context.TaskPMs.AnyAsync(t => t.Id == taskId && t.IsActive);
            if (!taskExists) return NotFound($"Task {taskId} not found.");

            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var url = req.Url.Trim();
            if (url.Length < 3) return BadRequest("Url is invalid.");

            // FileName: ha nincs, próbáljuk a linkből kinyerni
            var fileName = (req.FileName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                try
                {
                    var uri = new Uri(url, UriKind.RelativeOrAbsolute);
                    var last = uri.IsAbsoluteUri ? uri.LocalPath : url;
                    fileName = Path.GetFileName(last.Trim().TrimEnd('\\', '/'));
                }
                catch { /* ignore */ }

                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = "Hivatkozás";
            }

            // ✅ 1) próbáljuk meg a meglévő Document rekordot újrahasznosítani ugyanazzal a FilePath-tal
            var existingDocId = await _context.Documents
                .AsNoTracking()
                .Where(d => d.FilePath == url)
                .Select(d => (int?)d.DocumentId)
                .FirstOrDefaultAsync();

            int docId;
            if (existingDocId.HasValue)
            {
                docId = existingDocId.Value;
            }
            else
            {
                var doc = new Document
                {
                    FileName = fileName,
                    FilePath = url
                };

                _context.Documents.Add(doc);
                await _context.SaveChangesAsync();
                docId = doc.DocumentId;
            }

            // ✅ 2) duplikáció védelem a Task ↔ Document linkre
            var already = await _context.TaskDocumentLinks
                .AnyAsync(x => x.TaskId == taskId && x.DocumentId == docId);

            if (already)
            {
                return await GetAttachments(taskId);
            }

            var link = new TaskDocumentLink
            {
                TaskId = taskId,
                DocumentId = docId,
                LinkedDate = DateTime.UtcNow,
                LinkedById = userId,
                Note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim()
            };

            _context.TaskDocumentLinks.Add(link);
            await _context.SaveChangesAsync();

            return await GetAttachments(taskId);
        }


        [HttpDelete("{taskId:int}/attachments/{linkId:int}")]
        public async Task<IActionResult> RemoveAttachment(int taskId, int linkId)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var link = await _context.TaskDocumentLinks
                .FirstOrDefaultAsync(x => x.Id == linkId && x.TaskId == taskId);

            if (link == null) return NotFound();

            var docId = link.DocumentId;

            await using var tx = await _context.Database.BeginTransactionAsync();

            _context.TaskDocumentLinks.Remove(link);
            await _context.SaveChangesAsync();

            // ✅ opcionális: Document törlés, ha már sehol nem hivatkozzák
            var stillUsed = await _context.TaskDocumentLinks.AnyAsync(x => x.DocumentId == docId);
            if (!stillUsed)
            {
                // ⚠️ NEM olvassuk be a Document sort (itt hasalt el nálad)!
                var exists = await _context.Documents.AnyAsync(d => d.DocumentId == docId);
                if (exists)
                {
                    var stub = new Document { DocumentId = docId };
                    _context.Documents.Attach(stub);
                    _context.Documents.Remove(stub);
                    await _context.SaveChangesAsync();
                }
            }

            await tx.CommitAsync();

            return await GetAttachments(taskId);
        }



        [HttpPost("{taskId:int}/documents/upload")]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> UploadAndAttach(int taskId, IFormFile file, [FromQuery] string? note = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var taskExists = await _context.TaskPMs.AnyAsync(t => t.Id == taskId && t.IsActive);
            if (!taskExists) return NotFound($"Task {taskId} not found.");

            // DEV: fix folder
            var uploadsRoot = "/Users/tp/Projects/fileok";
            Directory.CreateDirectory(uploadsRoot);

            var safeOriginalName = Path.GetFileName(file.FileName);
            var storedName = $"{Guid.NewGuid():N}_{safeOriginalName}";
            var absPath = Path.Combine(uploadsRoot, storedName);

            await using (var fs = System.IO.File.Create(absPath))
                await file.CopyToAsync(fs);

            // amit a UI-n linkként akarsz megnyitni, az lehet egy "file://" url
            var fileUrl = "file://" + absPath;

            var doc = new Document
            {
                FileName = safeOriginalName,
                FilePath = fileUrl
            };

            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var link = new TaskDocumentLink
            {
                TaskId = taskId,
                DocumentId = doc.DocumentId,
                LinkedDate = DateTime.UtcNow,
                LinkedById = CurrentUserId,
                Note = note
            };

            _context.TaskDocumentLinks.Add(link);
            await _context.SaveChangesAsync();

            // add vissza a friss listát (a modalnak ez kell)
            return await GetAttachments(taskId);
        }

        // GET: api/tasks/{taskId}/audit
        [HttpGet("{taskId:int}/audit")]
        public async Task<IActionResult> GetTaskAudit(int taskId)
        {
            // csak TaskPM-hez tartozó audit logok
            var items = await _context.Set<AuditLog>()
                .AsNoTracking()
                .Where(a => a.EntityType == "TaskPM" && a.EntityId == taskId)
                .OrderByDescending(a => a.ChangedAt)
                .Select(a => new
                {
                    a.Action,
                    a.ChangedAt,
                    a.ChangedByName,
                    a.Changes
                })
                .ToListAsync();

            return Ok(items);
        }


        // GET: api/tasks/tasktypes/select?displayType=1
        [HttpGet("tasktypes/select")]
        public async Task<IActionResult> GetTaskTypesForSelect([FromQuery] int? displayType = null)
        {
            try
            {
                var q = _context.TaskTypePMs
                    .AsNoTracking()
                    .Where(t => t.IsActive == true);

                // opcionális szűrés
                if (displayType.HasValue)
                    q = q.Where(t => t.DisplayType == displayType.Value);

                var items = await q
                    .OrderBy(t => t.TaskTypePMName)
                    .Select(t => new { id = t.TaskTypePMId, text = t.TaskTypePMName })
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task types for select");
                return StatusCode(500, "An error occurred while retrieving task types.");
            }
        }

        // GET: api/tasks/taskstatuses/select?displayType=1
        [HttpGet("taskstatuses/select")]
        public async Task<IActionResult> GetTaskStatusesForSelect([FromQuery] int? displayType = null)
        {
            try
            {
                var q = _context.TaskStatusesPM
                    .AsNoTracking()
                    .Where(s => s.IsActive == true);

                // opcionális szűrés
                if (displayType.HasValue)
                    q = q.Where(s => s.DisplayType == displayType.Value);

                var items = await q
                    .OrderBy(s => s.DisplayOrder ?? 0)
                    .ThenBy(s => s.Name)
                    .Select(s => new { id = s.TaskStatusPMId, text = s.Name })
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task statuses for select");
                return StatusCode(500, "An error occurred while retrieving task statuses.");
            }
        }

    }


}