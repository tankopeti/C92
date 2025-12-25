using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditInterceptor> _logger;

        public AuditInterceptor(
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditInterceptor> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "system";

            _logger.LogDebug("AuditInterceptor - Current User ID: {UserId}", userId);
            return userId;
        }

        // Cache dictionary-k (requestenként újrainicializálódnak)
        private readonly Dictionary<int, string> _statusNames = new();
        private readonly Dictionary<int, string> _priorityNames = new();
        private readonly Dictionary<int, string> _typeNames = new();
        private readonly Dictionary<string, string> _userNames = new();
        private readonly Dictionary<int, string> _partnerNames = new();

        private async Task LoadLookupDataAsync(DbContext context)
        {
            if (!_statusNames.Any())
            {
                var statuses = await context.Set<TaskStatusPM>().ToListAsync();
                foreach (var s in statuses)
                    _statusNames[s.TaskStatusPMId] = s.Name ?? "Ismeretlen";
                _logger.LogDebug("Loaded {Count} TaskStatusPM names.", _statusNames.Count);
            }

            if (!_priorityNames.Any())
            {
                var priorities = await context.Set<TaskPriorityPM>().ToListAsync();
                foreach (var p in priorities)
                    _priorityNames[p.TaskPriorityPMId] = p.Name ?? "Ismeretlen";
            }

            if (!_typeNames.Any())
            {
                var types = await context.Set<TaskTypePM>().ToListAsync();
                foreach (var t in types)
                    _typeNames[t.TaskTypePMId] = t.TaskTypePMName ?? "Ismeretlen";
            }

            if (!_partnerNames.Any())
            {
                var partners = await context.Set<Partner>().ToListAsync();
                foreach (var p in partners)
                    _partnerNames[p.PartnerId] = p.Name ?? "Ismeretlen";
            }
        }

        private string GetStatusName(int? id) => id.HasValue && _statusNames.TryGetValue(id.Value, out var name) ? name : "Ismeretlen";
        private string GetPriorityName(int? id) => id.HasValue && _priorityNames.TryGetValue(id.Value, out var name) ? name : "Ismeretlen";
        private string GetTypeName(int? id) => id.HasValue && _typeNames.TryGetValue(id.Value, out var name) ? name : "Ismeretlen";
        private string GetPartnerName(int? id) => id.HasValue && _partnerNames.TryGetValue(id.Value, out var name) ? name : "Nincs";

        private async Task<string> GetUserNameAsync(DbContext context, string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return "Nincs";

            if (_userNames.TryGetValue(userId, out var cached))
                return cached;

            var userName = await context.Set<ApplicationUser>()
                .Where(u => u.Id == userId)
                .Select(u => u.UserName ?? "Ismeretlen felhasználó")
                .FirstOrDefaultAsync();

            _userNames[userId] = userName ?? "Ismeretlen felhasználó";
            return _userNames[userId];
        }

        // Most async Task visszatéréssel!
        private async Task AuditTaskChangesAsync(DbContext? context)
        {
            if (context == null)
            {
                _logger.LogWarning("AuditInterceptor - Context is null, skipping audit.");
                return;
            }

            _logger.LogInformation("AuditInterceptor - SavingChanges triggered.");

            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            await LoadLookupDataAsync(context);

            var modifiedTasks = context.ChangeTracker
                .Entries<TaskPM>()
                .Where(e => e.State == EntityState.Modified)
                .ToList();

            _logger.LogInformation("Found {Count} modified TaskPM entities.", modifiedTasks.Count);

            if (!modifiedTasks.Any())
            {
                _logger.LogDebug("No modified TaskPM entities – nothing to audit.");
                return;
            }

            foreach (var entry in modifiedTasks)
            {
                _logger.LogInformation("Auditing changes for TaskPM Id: {TaskId}", entry.Entity.Id);

                var changes = new List<string>();

                // Cím
                var oldTitle = entry.OriginalValues[nameof(TaskPM.Title)]?.ToString() ?? "null";
                var newTitle = entry.CurrentValues[nameof(TaskPM.Title)]?.ToString() ?? "null";
                if (oldTitle != newTitle)
                    changes.Add($"Cím: {oldTitle} → {newTitle}");

                // Leírás
                var oldDesc = entry.OriginalValues[nameof(TaskPM.Description)]?.ToString() ?? "null";
                var newDesc = entry.CurrentValues[nameof(TaskPM.Description)]?.ToString() ?? "null";
                if (oldDesc != newDesc)
                    changes.Add($"Leírás módosult");

                // Státusz
                var oldStatusId = entry.OriginalValues[nameof(TaskPM.TaskStatusPMId)] as int?;
                var newStatusId = entry.CurrentValues[nameof(TaskPM.TaskStatusPMId)] as int?;
                var oldStatusName = GetStatusName(oldStatusId);
                var newStatusName = GetStatusName(newStatusId);
                if (oldStatusName != newStatusName)
                    changes.Add($"Státusz: {oldStatusName} → {newStatusName}");

                // Prioritás
                var oldPriorityId = entry.OriginalValues[nameof(TaskPM.TaskPriorityPMId)] as int?;
                var newPriorityId = entry.CurrentValues[nameof(TaskPM.TaskPriorityPMId)] as int?;
                var oldPriorityName = GetPriorityName(oldPriorityId);
                var newPriorityName = GetPriorityName(newPriorityId);
                if (oldPriorityName != newPriorityName)
                    changes.Add($"Prioritás: {oldPriorityName} → {newPriorityName}");

                // Feladat típus
                var oldTypeId = entry.OriginalValues[nameof(TaskPM.TaskTypePMId)] as int?;
                var newTypeId = entry.CurrentValues[nameof(TaskPM.TaskTypePMId)] as int?;
                var oldTypeName = GetTypeName(oldTypeId);
                var newTypeName = GetTypeName(newTypeId);
                if (oldTypeName != newTypeName)
                    changes.Add($"Feladat típus: {oldTypeName} → {newTypeName}");

                // Felelős
                var oldAssignedId = entry.OriginalValues[nameof(TaskPM.AssignedToId)] as string;
                var newAssignedId = entry.CurrentValues[nameof(TaskPM.AssignedToId)] as string;
                if (oldAssignedId != newAssignedId)
                {
                    var oldAssignedName = await GetUserNameAsync(context, oldAssignedId);
                    var newAssignedName = await GetUserNameAsync(context, newAssignedId);
                    changes.Add($"Felelős: {oldAssignedName} → {newAssignedName}");
                }

                // Partner
                var oldPartnerId = entry.OriginalValues[nameof(TaskPM.PartnerId)] as int?;
                var newPartnerId = entry.CurrentValues[nameof(TaskPM.PartnerId)] as int?;
                var oldPartnerName = GetPartnerName(oldPartnerId);
                var newPartnerName = GetPartnerName(newPartnerId);
                if (oldPartnerName != newPartnerName)
                    changes.Add($"Partner: {oldPartnerName} → {newPartnerName}");

// Határidő
var oldDueRaw = entry.OriginalValues[nameof(TaskPM.DueDate)] as DateTime?;
var newDueRaw = entry.CurrentValues[nameof(TaskPM.DueDate)] as DateTime?;

var oldDue = oldDueRaw.HasValue ? oldDueRaw.Value.ToString("yyyy-MM-dd") : "nincs";
var newDue = newDueRaw.HasValue ? newDueRaw.Value.ToString("yyyy-MM-dd") : "nincs";

if (oldDue != newDue)
    changes.Add($"Határidő: {oldDue} → {newDue}");
    

                if (changes.Any())
                {
                    var description = string.Join("; ", changes);

                    var history = new TaskHistory
                    {
                        TaskPMId = entry.Entity.Id,
                        ModifiedById = userId,
                        ModifiedDate = now,
                        ChangeDescription = description
                    };

                    context.Set<TaskHistory>().Add(history);

                    _logger.LogInformation("TaskHistory created for TaskPM {TaskId} by {UserId}. Changes: {Changes}",
                        entry.Entity.Id, userId, description);
                }
                else
                {
                    _logger.LogInformation("No value changes for TaskPM {TaskId} despite Modified state.", entry.Entity.Id);
                }
            }
        }

        // Szinkron verzió – blokkoló hívás (ritkán használják webappokban, de kell)
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            _logger.LogDebug("AuditInterceptor - Synchronous SavingChanges called.");
            AuditTaskChangesAsync(eventData.Context).GetAwaiter().GetResult();
            return base.SavingChanges(eventData, result);
        }

        // Aszinkron verzió – ez fut webes környezetben
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("AuditInterceptor - Asynchronous SavingChangesAsync called.");
            await AuditTaskChangesAsync(eventData.Context);
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}