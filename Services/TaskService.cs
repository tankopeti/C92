using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Services
{
    public class TaskPMService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TaskPMService> _logger;

        public TaskPMService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<TaskPMService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region === Helpers ===
        private static string FullName(ApplicationUser? user) =>
            user == null ? null! : user.UserName;   // <-- only property that ALWAYS exists

        private static IQueryable<TaskPM> BaseQuery(ApplicationDbContext ctx) =>
            ctx.TaskPMs                                 // <-- **TaskPMs** (plural)
                .AsNoTracking()
                .Include(t => t.TaskTypePM)
                .Include(t => t.TaskStatusPM)
                .Include(t => t.TaskPriorityPM)
                // .Include(t => t.ProjectPM)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Partner)
                .Include(t => t.Site)
                .Include(t => t.Contact)
                .Include(t => t.Quote)
                .Include(t => t.Order)
                .Include(t => t.CustomerCommunication)
                .Include(t => t.CommunicationType)
                .Include(t => t.TaskResourceAssignments).ThenInclude(ra => ra.Resource)
                .Include(t => t.TaskEmployeeAssignments).ThenInclude(ea => ea.Employee)
                .Include(t => t.TaskHistories).ThenInclude(th => th.ModifiedBy)
                .Include(t => t.TaskDocuments)
            .ThenInclude(td => td.Document);
        #endregion

        // -----------------------------------------------------------------
        // GET ALL
        // -----------------------------------------------------------------
        public async Task<List<TaskPMDto>> GetAllTasksAsync()
        {
            return await BaseQuery(_context)
                .Where(t => t.IsActive)
                .Select(t => MapToDto(t))
                .ToListAsync();
        }

        // -----------------------------------------------------------------
        // GET BY ID
        // -----------------------------------------------------------------
        public async Task<TaskPMDto?> GetTaskByIdAsync(int id)
        {
            var task = await BaseQuery(_context)
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

            return task == null ? null : MapToDto(task);
        }

        // -----------------------------------------------------------------
        // CREATE
        // -----------------------------------------------------------------
        public async Task<TaskPMDto> CreateTaskAsync(TaskCreateDto dto, string currentUserId)
        {
            _logger.LogInformation("CreateTaskAsync started - User: {UserId}, DTO: {@DTO}", currentUserId, dto);

            // 1. VALIDATION
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ValidationException("A cím megadása kötelező.");

            if (dto.TaskTypePMId == null)
                throw new ValidationException("A feladat típusának kiválasztása kötelező.");

            // Check if the required Task Type ID actually exists to prevent Foreign Key failure
            var taskTypeExists = await _context.TaskTypePMs
                .AnyAsync(t => t.TaskTypePMId == dto.TaskTypePMId.Value);

            if (!taskTypeExists)
            {
                _logger.LogError("Invalid TaskTypePMId provided: {Id}", dto.TaskTypePMId.Value);
                throw new ValidationException("A kiválasztott feladat típus nem létezik.");
            }

            // Site validation (Existing logic for conditional Site dependency)
            if (dto.SiteId.HasValue && dto.SiteId.Value != 0)
            {
                if (dto.PartnerId == null || dto.PartnerId.Value == 0)
                    throw new ValidationException("Helyszín megadásakor Partner kiválasztása kötelező.");

                var siteExists = await _context.Sites.AnyAsync(s => s.SiteId == dto.SiteId.Value && s.PartnerId == dto.PartnerId);
                if (!siteExists)
                    throw new ValidationException("A Site nem tartozik a kiválasztott Partnerhez vagy nem létezik.");
            }

            // Map DTO to Entity
            var task = new TaskPM
            {
                Title = dto.Title,
                Description = dto.Description,
                IsActive = dto.IsActive,
                TaskTypePMId = dto.TaskTypePMId.Value, // Safe now
                TaskStatusPMId = dto.TaskStatusPMId ?? 1,
                TaskPriorityPMId = dto.TaskPriorityPMId ?? 2,
                DueDate = dto.DueDate,
                EstimatedHours = dto.EstimatedHours,
                ActualHours = dto.ActualHours,
                AssignedToId = dto.AssignedToId,
                ScheduledDate = dto.ScheduledDate,
                CommunicationTypeId = dto.CommunicationTypeId,
                CommunicationDescription = dto.CommunicationDescription,

                // Defensive check for optional integer FKs (TomSelect sometimes sends 0 for blank)
                PartnerId = dto.PartnerId == 0 ? null : dto.PartnerId,
                SiteId = dto.SiteId == 0 ? null : dto.SiteId,
                ContactId = dto.ContactId == 0 ? null : dto.ContactId,
                QuoteId = dto.QuoteId == 0 ? null : dto.QuoteId,
                OrderId = dto.OrderId == 0 ? null : dto.OrderId,
                CustomerCommunicationId = dto.CustomerCommunicationId == 0 ? null : dto.CustomerCommunicationId,

                CreatedById = currentUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            // 2. TRANSACTION BLOCK
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Adding task to context - Title: {Title}", task.Title);
                _context.TaskPMs.Add(task);
                // Save the task first to get its ID (Task.Id)
                await _context.SaveChangesAsync();
                _logger.LogInformation("Task saved - Id: {Id}", task.Id);

                // --- Resources Assignment (with validation) ---
                if (dto.ResourceIds?.Any() == true)
                {
                    // Filter out any ResourceIds that do not exist in the database
                    var validResourceIds = await _context.Resources
                        .Where(r => dto.ResourceIds.Contains(r.ResourceId))
                        .Select(r => r.ResourceId)
                        .ToListAsync();

                    if (validResourceIds.Any())
                    {
                        var res = validResourceIds.Select(rid => new TaskResourceAssignment
                        {
                            TaskPMId = task.Id,
                            ResourceId = rid
                        });
                        _logger.LogInformation("Adding {Count} valid resources", res.Count());
                        _context.TaskResourceAssignments.AddRange(res);
                    }
                }

                // --- Employees Assignment (with validation) ---
                if (dto.EmployeeIds?.Any() == true)
                {
                    // Filter out any EmployeeIds that do not exist in the database
                    var validEmployeeIds = await _context.Employees
                        .Where(e => dto.EmployeeIds.Contains(e.EmployeeId))
                        .Select(e => e.EmployeeId)
                        .ToListAsync();

                    if (validEmployeeIds.Any())
                    {
                        var emp = validEmployeeIds.Select(eid => new TaskEmployeeAssignment
                        {
                            TaskPMId = task.Id,
                            EmployeeId = eid
                        });
                        _logger.LogInformation("Adding {Count} valid employees", emp.Count());
                        _context.TaskEmployeeAssignments.AddRange(emp);
                    }
                }

                await _context.SaveChangesAsync(); // Save assignments (should be safe now)
                await transaction.CommitAsync();   // Commit the transaction

                _logger.LogInformation("Task with assignments saved successfully - Id: {Id}", task.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // CRITICAL: This is the log you MUST check now for the detailed error message
                _logger.LogError(ex, "DATABASE ERROR in CreateTaskAsync for Task: {Title} by User: {UserId}.", task.Title, currentUserId);

                throw; // Re-throw for the controller to catch and convert to HTTP 500
            }

            // 3. MAP AND RETURN DTO (Ensure you pull names for the return DTO if needed)
            return await GetTaskByIdAsync(task.Id)
                   ?? throw new InvalidOperationException("Failed to retrieve the newly created task.");
        }

        // -----------------------------------------------------------------
        // UPDATE
        // -----------------------------------------------------------------
public async Task<TaskPMDto> UpdateTaskAsync(TaskUpdateDto dto, string currentUserId)
{
    var task = await _context.TaskPMs
        .Include(t => t.TaskResourceAssignments)
        .Include(t => t.TaskEmployeeAssignments)
                .FirstOrDefaultAsync(t => t.Id == dto.Id && t.IsActive)
                ?? throw new KeyNotFoundException($"Task {dto.Id} not found.");

            task.Title = dto.Title;
            task.Description = dto.Description;
            // task.IsActive = dto.IsActive ?? task.IsActive;
            task.TaskTypePMId = dto.TaskTypePMId ?? task.TaskTypePMId;
            // task.ProjectPMId = dto.ProjectPMId ?? task.ProjectPMId;
            task.TaskStatusPMId = dto.TaskStatusPMId ?? task.TaskStatusPMId;
            task.TaskPriorityPMId = dto.TaskPriorityPMId ?? task.TaskPriorityPMId;
            task.DueDate = dto.DueDate ?? task.DueDate;
            task.EstimatedHours = dto.EstimatedHours ?? task.EstimatedHours;
            task.ActualHours = dto.ActualHours ?? task.ActualHours;
            task.AssignedToId = dto.AssignedToId ?? task.AssignedToId;
            task.PartnerId = dto.PartnerId ?? task.PartnerId;
            task.SiteId = dto.SiteId ?? task.SiteId;
            task.ContactId = dto.ContactId ?? task.ContactId;
            task.QuoteId = dto.QuoteId ?? task.QuoteId;
            task.OrderId = dto.OrderId ?? task.OrderId;
            task.ScheduledDate = dto.ScheduledDate ?? task.ScheduledDate;
            task.CommunicationTypeId = dto.CommunicationTypeId;
            task.CustomerCommunicationId = dto.CustomerCommunicationId ?? task.CustomerCommunicationId;
            task.CommunicationDescription = dto.CommunicationDescription;
            task.UpdatedDate = DateTime.UtcNow;

            // CompletedDate logic
            if (task.TaskStatusPMId == 3 && task.CompletedDate == null)   // 3 = Completed
                task.CompletedDate = DateTime.UtcNow;
            else if (task.TaskStatusPMId != 3)
                task.CompletedDate = null;

            // Resources
            if (dto.ResourceIds.Any())
            {
                var validResourceIds = await _context.Resources
                    .Where(r => dto.ResourceIds.Contains(r.ResourceId))
                    .Select(r => r.ResourceId)
                    .ToListAsync();

                if (validResourceIds.Any())
                {
                    var newRes = validResourceIds.Select(rid => new TaskResourceAssignment
                    {
                        TaskPMId = task.Id,
                        ResourceId = rid
                    });
                    _context.TaskResourceAssignments.AddRange(newRes);
                }
            }


            // Employees
            if (dto.EmployeeIds != null)
            {
                // Step 1: Remove existing assignments
                _context.TaskEmployeeAssignments.RemoveRange(task.TaskEmployeeAssignments);

                // Step 2: Filter and Add NEW assignments (only those with valid IDs)
                if (dto.EmployeeIds.Any())
                {
                    var validEmployeeIds = await _context.Employees
                        .Where(e => dto.EmployeeIds.Contains(e.EmployeeId))
                        .Select(e => e.EmployeeId)
                        .ToListAsync();

                    if (validEmployeeIds.Any())
                    {
                        var newEmp = validEmployeeIds.Select(eid => new TaskEmployeeAssignment
                        {
                            TaskPMId = task.Id,
                            EmployeeId = eid
                        });
                        _context.TaskEmployeeAssignments.AddRange(newEmp);
                    }
                }
            }
    
            await _context.SaveChangesAsync();

            return await GetTaskByIdAsync(task.Id)
                   ?? throw new ValidationException("Failed to retrieve updated task.");
        }

        // -----------------------------------------------------------------
        // SOFT DELETE
        // -----------------------------------------------------------------
        public async Task DeleteTaskAsync(int id)
        {
            var task = await _context.TaskPMs
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive)
                ?? throw new KeyNotFoundException($"Task {id} not found.");

            task.IsActive = false;
            task.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // -----------------------------------------------------------------
        // PAGED + FILTER + SEARCH + SORT
        // -----------------------------------------------------------------
        public async Task<PagedResult<TaskPMDto>> GetPagedTasksAsync(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? sort = null,
            string? order = "desc",
            int? statusId = null,
            int? priorityId = null,
            int? taskTypeId = null,           // ÚJ: Feladat típusa / Task Type
            int? partnerId = null,            // ÚJ: Partner
            int? siteId = null,               // ÚJ: Telephely / Site
            string? assignedToId = null,      // Technikus / Technician
            DateTime? dueDateFrom = null,     // Határidő tól / Due Date From
            DateTime? dueDateTo = null,       // Határidő ig / Due Date To
            DateTime? createdDateFrom = null, // Létrehozás tól / Created From
            DateTime? createdDateTo = null)   // Létrehozás ig / Created To
        {
            // --- Biztonságos értékek ---
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize));
            sort ??= "Id";
            order ??= "desc";

            // --- Alap lekérdezés ---
            var query = BaseQuery(_context).Where(t => t.IsActive);

            // === SZŰRŐK / FILTERS ===
            if (statusId.HasValue) query = query.Where(t => t.TaskStatusPMId == statusId.Value);
            if (priorityId.HasValue) query = query.Where(t => t.TaskPriorityPMId == priorityId.Value);
            if (taskTypeId.HasValue) query = query.Where(t => t.TaskTypePMId == taskTypeId.Value);
            if (partnerId.HasValue) query = query.Where(t => t.PartnerId == partnerId.Value);
            if (siteId.HasValue) query = query.Where(t => t.SiteId == siteId.Value);
            if (!string.IsNullOrWhiteSpace(assignedToId))
                query = query.Where(t => t.AssignedToId == assignedToId);

            // Dátumtartományok / Date ranges
            if (dueDateFrom.HasValue)
                query = query.Where(t => t.DueDate >= dueDateFrom.Value.Date);

            if (dueDateTo.HasValue)
                query = query.Where(t => t.DueDate <= dueDateTo.Value.Date.AddDays(1).AddSeconds(-1));

            if (createdDateFrom.HasValue)
                query = query.Where(t => t.CreatedDate >= createdDateFrom.Value.Date);

            if (createdDateTo.HasValue)
                query = query.Where(t => t.CreatedDate <= createdDateTo.Value.Date.AddDays(1).AddSeconds(-1));

            // === KERESÉS / SEARCH ===
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(term) ||
                    (t.Description != null && t.Description.ToLower().Contains(term)) ||
                    (t.AssignedTo != null && t.AssignedTo.UserName.ToLower().Contains(term)) ||
                    (t.CreatedBy != null && t.CreatedBy.UserName.ToLower().Contains(term)) ||
                    (t.Site != null && t.Site.SiteName.ToLower().Contains(term)) ||
                    (t.Site != null && t.Site.City.ToLower().Contains(term)));
            }

            // --- Összes találat száma (lapozás előtt!) ---
            var totalCount = await query.CountAsync();

            // === RENDEZÉS / SORTING ===
            query = sort.ToLowerInvariant() switch
            {
                "title" => order == "desc" ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
                "duedate" => order == "desc" ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
                "status" => order == "desc" ? query.OrderByDescending(t => t.TaskStatusPM!.Name) : query.OrderBy(t => t.TaskStatusPM!.Name),
                "priority" => order == "desc" ? query.OrderByDescending(t => t.TaskPriorityPM!.Name) : query.OrderBy(t => t.TaskPriorityPM!.Name),
                "assignedto" => order == "desc" ? query.OrderByDescending(t => t.AssignedTo!.UserName) : query.OrderBy(t => t.AssignedTo!.UserName),
                "createddate" => order == "desc" ? query.OrderByDescending(t => t.CreatedDate) : query.OrderBy(t => t.CreatedDate),
                "partner" => order == "desc" ? query.OrderByDescending(t => t.Partner!.Name) : query.OrderBy(t => t.Partner!.Name),
                "site" => order == "desc" ? query.OrderByDescending(t => t.Site!.SiteName) : query.OrderBy(t => t.Site!.SiteName),
                "tasktype" => order == "desc" ? query.OrderByDescending(t => t.TaskTypePM!.TaskTypePMName) : query.OrderBy(t => t.TaskTypePM!.TaskTypePMName),
                _ => order == "desc" ? query.OrderByDescending(t => t.Id) : query.OrderBy(t => t.Id)
            };

            // --- Lapozás / Pagination ---
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => MapToDto(t))
                .ToListAsync();

            // --- Visszatérés ---
            return new PagedResult<TaskPMDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // -----------------------------------------------------------------
        // MAP ENTITY → DTO
        // -----------------------------------------------------------------
        private static TaskPMDto MapToDto(TaskPM task)
        {
            return new TaskPMDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                IsActive = task.IsActive,

                TaskTypePMId = task.TaskTypePMId,
                TaskTypePMName = task.TaskTypePM?.TaskTypePMName,

                TaskStatusPMId = task.TaskStatusPMId,
                TaskStatusPMName = task.TaskStatusPM?.Name,
                ColorCode = task.TaskStatusPM?.ColorCode,

                TaskPriorityPMId = task.TaskPriorityPMId,
                TaskPriorityPMName = task.TaskPriorityPM?.Name,

                CommunicationTypeId = task.CommunicationTypeId,
                CommunicationTypeName = task.CommunicationType?.Name,
                CommunicationDescription = task.CommunicationDescription,

                DueDate = task.DueDate,
                EstimatedHours = task.EstimatedHours,
                ActualHours = task.ActualHours,
                ScheduledDate = task.ScheduledDate,

                CreatedById = task.CreatedById,
                CreatedByName = FullName(task.CreatedBy),

                CreatedDate = task.CreatedDate,

                AssignedToId = task.AssignedToId,
                AssignedToName = FullName(task.AssignedTo),

                UpdatedDate = task.UpdatedDate,
                CompletedDate = task.CompletedDate,

                // ProjectPMId = task.ProjectPMId,
                // ProjectPMName = task.ProjectPM?.Name,

                PartnerId = task.PartnerId,
                PartnerName = task.Partner?.Name,

                SiteId = task.SiteId,
                SiteName = task.Site?.SiteName,
                City = task.Site?.City,

                ContactId = task.ContactId,
                ContactName = task.Contact != null
                    ? $"{task.Contact.FirstName} {task.Contact.LastName}".Trim()
                    : null,

                QuoteId = task.QuoteId,
                QuoteNumber = task.Quote?.QuoteNumber,

                OrderId = task.OrderId,
                OrderNumber = task.Order?.OrderNumber,

                CustomerCommunicationId = task.CustomerCommunicationId,
                CustomerCommunicationSubject = task.CustomerCommunication?.Subject,

                ResourceIds = task.TaskResourceAssignments.Select(ra => ra.ResourceId).ToList(),
                EmployeeIds = task.TaskEmployeeAssignments.Select(ea => ea.EmployeeId).ToList(),

                // ----- History -----
                TaskHistories = task.TaskHistories
                    .Select(th => new TaskHistoryDto
                    {
                        TaskHistoryId = th.TaskHistoryId,
                        TaskPMId = th.TaskPMId,
                        ModifiedById = th.ModifiedById,
                        ModifiedByName = FullName(th.ModifiedBy),
                        ModifiedDate = th.ModifiedDate,
                        ChangeDescription = th.ChangeDescription
                    })
                    .OrderByDescending(th => th.ModifiedDate)
                    .ToList(),

                Attachments = task.TaskDocuments
            .Select(td => new TaskDocumentDto
            {
                Id = td.Id,
                DocumentId = td.DocumentId,
                FileName = td.Document.FileName,
                FilePath = td.Document.FilePath,
                LinkedDate = td.LinkedDate,
                LinkedByName = td.LinkedBy != null ? td.LinkedBy.UserName : null,
                Note = td.Note
            })
            .OrderByDescending(a => a.LinkedDate)
            .ToList()

            };
        }

        // Attach existing document to task
        public async Task AttachDocumentAsync(int taskId, int documentId, string currentUserId, string? note = null)
        {
            if (!await _context.TaskPMs.AnyAsync(t => t.Id == taskId && t.IsActive))
                throw new KeyNotFoundException($"Task {taskId} not found.");

            if (!await _context.Documents.AnyAsync(d => d.DocumentId == documentId))
                throw new KeyNotFoundException($"Document {documentId} not found.");

            var alreadyAttached = await _context.TaskDocumentLinks
                .AnyAsync(x => x.TaskId == taskId && x.DocumentId == documentId);

            if (alreadyAttached)
                return;

            var link = new TaskDocumentLink
            {
                TaskId = taskId,
                DocumentId = documentId,
                LinkedDate = DateTime.UtcNow,
                LinkedById = currentUserId,
                Note = note
            };

            _context.TaskDocumentLinks.Add(link);
            await _context.SaveChangesAsync();
        }

        // Remove document from task
        public async Task RemoveDocumentAsync(int taskId, int documentLinkId, string currentUserId)
        {
            var link = await _context.TaskDocumentLinks
                .FirstOrDefaultAsync(x => x.Id == documentLinkId && x.TaskId == taskId);

            if (link == null)
                throw new KeyNotFoundException($"Attachment {documentLinkId} not found on task {taskId}");

            _context.TaskDocumentLinks.Remove(link);
            await _context.SaveChangesAsync();
        }


        // Optional: Attach multiple at once
        public async Task AttachDocumentsAsync(int taskId, List<int> documentIds, string currentUserId, string? note = null)
        {
            foreach (var docId in documentIds.Distinct())
            {
                await AttachDocumentAsync(taskId, docId, currentUserId, note);
            }
        }

    }

    // -----------------------------------------------------------------
    // PAGED RESULT
    // -----------------------------------------------------------------
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}