using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Cloud9_2.Interceptors
{
    public class GenericAuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GenericAuditInterceptor> _logger;

        // Mely entitásokat auditáljunk?
        // ✅ Partner + TaskPM marad
        // ✅ + CustomerCommunication + CommunicationResponsible hozzáadva
        private static readonly Dictionary<Type, string> AuditedEntities = new()
        {
            { typeof(Partner), "Partner" },
            { typeof(TaskPM), "TaskPM" },

            { typeof(CustomerCommunication), "CustomerCommunication" },
            { typeof(CommunicationResponsible), "CommunicationResponsible" }
        };

        // Kizárjuk ezeket a property-ket az auditból (pl. automatikus dátumok)
        private static readonly HashSet<string> ExcludedProperties = new()
        {
            "CreatedDate", "ModifiedDate", "IsActive", "RowVersion"
        };

        public GenericAuditInterceptor(IHttpContextAccessor httpContextAccessor, ILogger<GenericAuditInterceptor> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        }

        private async Task<string> GetUserNameAsync(DbContext context, string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return "Rendszer";

            var user = await context.Set<ApplicationUser>()
                .Where(u => u.Id == userId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync();

            return user ?? "Ismeretlen felhasználó";
        }

        private async Task<string> GetPartnerNameAsync(DbContext context, int? partnerId)
        {
            if (!partnerId.HasValue) return "—";
            // igazítsd, ha nálad más a mezőnév (Name/CompanyName/PartnerName)
            var name = await context.Set<Partner>()
                .Where(p => p.PartnerId == partnerId.Value)
                .Select(p => p.CompanyName ?? p.Name ?? "")
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(name) ? $"#{partnerId.Value}" : name;
        }

        private async Task<string> GetSiteNameAsync(DbContext context, int? siteId)
        {
            if (!siteId.HasValue) return "—";
            // igazítsd, ha nálad más a mezőnév (SiteName/Name)
            var name = await context.Set<Site>()
                .Where(s => s.SiteId == siteId.Value)
                .Select(s => s.SiteName ?? s.SiteName ?? "")
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(name) ? $"#{siteId.Value}" : name;
        }

        private async Task<string> GetCommunicationTypeNameAsync(DbContext context, int typeId)
        {
            if (typeId <= 0) return "—";
            var name = await context.Set<CommunicationType>()
                .Where(t => t.CommunicationTypeId == typeId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();
            return string.IsNullOrWhiteSpace(name) ? $"#{typeId}" : name;
        }

        private async Task<string> GetCommunicationStatusNameAsync(DbContext context, int statusId)
        {
            if (statusId <= 0) return "—";
            var name = await context.Set<CommunicationStatus>()
                .Where(s => s.StatusId == statusId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();
            return string.IsNullOrWhiteSpace(name) ? $"#{statusId}" : name;
        }

        private async Task AuditChangesAsync(DbContext context)
        {
            var userId = GetCurrentUserId();
            var userName = await GetUserNameAsync(context, userId);
            var now = DateTime.UtcNow;

            var auditEntries = new List<AuditLog>();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (!AuditedEntities.TryGetValue(entry.Entity.GetType(), out var entityTypeName))
                    continue;

                int entityId = GetEntityId(entry);

                switch (entry.State)
                {
                    case EntityState.Added:
                    {
                        // ✅ Partner/TaskPM szöveg marad "mostani" szerint
                        if (entry.Entity is Partner)
                        {
                            auditEntries.Add(new AuditLog
                            {
                                EntityType = entityTypeName,
                                EntityId = entityId,
                                Action = "Created",
                                ChangedById = userId,
                                ChangedByName = userName,
                                ChangedAt = now,
                                Changes = "Új partner létrehozva."
                            });
                            break;
                        }

                        // ✅ CustomerCommunication: szép, értelmezhető "Created"
                        if (entry.Entity is CustomerCommunication ccNew)
                        {
                            var typeName = await GetCommunicationTypeNameAsync(context, ccNew.CommunicationTypeId);
                            var statusName = await GetCommunicationStatusNameAsync(context, ccNew.StatusId);
                            var partnerName = await GetPartnerNameAsync(context, ccNew.PartnerId);
                            var siteName = await GetSiteNameAsync(context, ccNew.SiteId);

                            auditEntries.Add(new AuditLog
                            {
                                EntityType = entityTypeName,
                                EntityId = entityId,
                                Action = "Created",
                                ChangedById = userId,
                                ChangedByName = userName,
                                ChangedAt = now,
                                Changes =
                                    $"Új kommunikáció létrehozva. " +
                                    $"Típus: {typeName}; Státusz: {statusName}; Partner: {partnerName}; Telephely: {siteName}; " +
                                    $"Tárgy: {ccNew.Subject ?? "—"}"
                            });
                            break;
                        }

                        // ✅ CommunicationResponsible: felelős kiosztás
                        if (entry.Entity is CommunicationResponsible crNew)
                        {
                            var responsibleName = await GetUserNameAsync(context, crNew.ResponsibleId);
                            auditEntries.Add(new AuditLog
                            {
                                EntityType = entityTypeName,
                                EntityId = entityId,
                                Action = "Created",
                                ChangedById = userId,
                                ChangedByName = userName,
                                ChangedAt = now,
                                Changes = $"Felelős kiosztva. Kommunikáció: #{crNew.CustomerCommunicationId}; Felelős: {responsibleName}"
                            });
                            break;
                        }

                        // fallback (TaskPM vagy más később)
                        auditEntries.Add(new AuditLog
                        {
                            EntityType = entityTypeName,
                            EntityId = entityId,
                            Action = "Created",
                            ChangedById = userId,
                            ChangedByName = userName,
                            ChangedAt = now,
                            Changes = "Új rekord létrehozva."
                        });
                        break;
                    }

                    case EntityState.Modified:
                    {
                        var changes = new List<string>();

                        foreach (var prop in entry.Properties)
                        {
                            if (ExcludedProperties.Contains(prop.Metadata.Name))
                                continue;

                            var oldValue = entry.OriginalValues[prop.Metadata]?.ToString() ?? "null";
                            var newValue = entry.CurrentValues[prop.Metadata]?.ToString() ?? "null";

                            if (oldValue == newValue)
                                continue;

                            // ✅ Partner mezőnevek maradnak változatlanul (a mostani szerint)
                            if (entry.Entity is Partner)
                            {
                                string displayName = prop.Metadata.Name switch
                                {
                                    "Name" => "Név",
                                    "CompanyName" => "Cégnév",
                                    "TaxId" => "Adószám",
                                    "Email" => "E-mail",
                                    "Phone" => "Telefon",
                                    "Website" => "Weboldal",
                                    "StatusId" => "Státusz",
                                    "PartnerTypeId" => "Partner típus",
                                    "BillingAddress" => "Számlázási cím",
                                    "BillingName" => "Számlázási név",
                                    "BillingTaxId" => "Számlázási adószám",
                                    "Notes" => "Jegyzetek",
                                    _ => prop.Metadata.Name
                                };

                                changes.Add($"{displayName}: {oldValue} → {newValue}");
                                continue;
                            }

                            // ✅ CustomerCommunication: pár mező szépítése + id->név (type/status/partner/site)
                            if (entry.Entity is CustomerCommunication)
                            {
                                string displayName = prop.Metadata.Name switch
                                {
                                    "Subject" => "Tárgy",
                                    "Note" => "Tartalom",
                                    "Metadata" => "Megjegyzések",
                                    "Date" => "Dátum",
                                    "PartnerId" => "Partner",
                                    "SiteId" => "Telephely",
                                    "CommunicationTypeId" => "Típus",
                                    "StatusId" => "Státusz",
                                    "AgentId" => "Ügyintéző",
                                    _ => prop.Metadata.Name
                                };

                                // id->név fordítás néhány kulcsnál
                                if (prop.Metadata.Name == "PartnerId")
                                {
                                    var oldP = int.TryParse(oldValue, out var op) ? (int?)op : null;
                                    var newP = int.TryParse(newValue, out var np) ? (int?)np : null;
                                    var oldName = await GetPartnerNameAsync(context, oldP);
                                    var newName = await GetPartnerNameAsync(context, newP);
                                    changes.Add($"{displayName}: {oldName} → {newName}");
                                    continue;
                                }

                                if (prop.Metadata.Name == "SiteId")
                                {
                                    var oldS = int.TryParse(oldValue, out var os) ? (int?)os : null;
                                    var newS = int.TryParse(newValue, out var ns) ? (int?)ns : null;
                                    var oldName = await GetSiteNameAsync(context, oldS);
                                    var newName = await GetSiteNameAsync(context, newS);
                                    changes.Add($"{displayName}: {oldName} → {newName}");
                                    continue;
                                }

                                if (prop.Metadata.Name == "CommunicationTypeId")
                                {
                                    var oldT = int.TryParse(oldValue, out var ot) ? ot : 0;
                                    var newT = int.TryParse(newValue, out var nt) ? nt : 0;
                                    var oldName = await GetCommunicationTypeNameAsync(context, oldT);
                                    var newName = await GetCommunicationTypeNameAsync(context, newT);
                                    changes.Add($"{displayName}: {oldName} → {newName}");
                                    continue;
                                }

                                if (prop.Metadata.Name == "StatusId")
                                {
                                    var oldSt = int.TryParse(oldValue, out var ost) ? ost : 0;
                                    var newSt = int.TryParse(newValue, out var nst) ? nst : 0;
                                    var oldName = await GetCommunicationStatusNameAsync(context, oldSt);
                                    var newName = await GetCommunicationStatusNameAsync(context, newSt);
                                    changes.Add($"{displayName}: {oldName} → {newName}");
                                    continue;
                                }

                                if (prop.Metadata.Name == "AgentId")
                                {
                                    var oldName = await GetUserNameAsync(context, oldValue == "null" ? null : oldValue);
                                    var newName = await GetUserNameAsync(context, newValue == "null" ? null : newValue);
                                    changes.Add($"{displayName}: {oldName} → {newName}");
                                    continue;
                                }

                                changes.Add($"{displayName}: {oldValue} → {newValue}");
                                continue;
                            }

                            // ✅ CommunicationResponsible: felelős csere logolása (ha módosulna)
                            if (entry.Entity is CommunicationResponsible && prop.Metadata.Name == "ResponsibleId")
                            {
                                var oldName = await GetUserNameAsync(context, oldValue == "null" ? null : oldValue);
                                var newName = await GetUserNameAsync(context, newValue == "null" ? null : newValue);
                                changes.Add($"Felelős: {oldName} → {newName}");
                                continue;
                            }

                            // default
                            changes.Add($"{prop.Metadata.Name}: {oldValue} → {newValue}");
                        }

                        if (changes.Any())
                        {
                            auditEntries.Add(new AuditLog
                            {
                                EntityType = entityTypeName,
                                EntityId = entityId,
                                Action = "Updated",
                                ChangedById = userId,
                                ChangedByName = userName,
                                ChangedAt = now,
                                Changes = string.Join("; ", changes)
                            });
                        }
                        break;
                    }

                    case EntityState.Deleted:
                    {
                        // ✅ Partner szöveg marad változatlanul
                        if (entry.Entity is Partner)
                        {
                            auditEntries.Add(new AuditLog
                            {
                                EntityType = entityTypeName,
                                EntityId = entityId,
                                Action = "Deleted",
                                ChangedById = userId,
                                ChangedByName = userName,
                                ChangedAt = now,
                                Changes = "Partner törölve (soft delete)."
                            });
                            break;
                        }

                        if (entry.Entity is CustomerCommunication)
                        {
                            auditEntries.Add(new AuditLog
                            {
                                EntityType = entityTypeName,
                                EntityId = entityId,
                                Action = "Deleted",
                                ChangedById = userId,
                                ChangedByName = userName,
                                ChangedAt = now,
                                Changes = "Kommunikáció törölve."
                            });
                            break;
                        }

                        if (entry.Entity is CommunicationResponsible crDel)
                        {
                            var responsibleName = await GetUserNameAsync(context, crDel.ResponsibleId);
                            auditEntries.Add(new AuditLog
                            {
                                EntityType = entityTypeName,
                                EntityId = entityId,
                                Action = "Deleted",
                                ChangedById = userId,
                                ChangedByName = userName,
                                ChangedAt = now,
                                Changes = $"Felelős törölve. Kommunikáció: #{crDel.CustomerCommunicationId}; Felelős: {responsibleName}"
                            });
                            break;
                        }

                        // fallback
                        auditEntries.Add(new AuditLog
                        {
                            EntityType = entityTypeName,
                            EntityId = entityId,
                            Action = "Deleted",
                            ChangedById = userId,
                            ChangedByName = userName,
                            ChangedAt = now,
                            Changes = "Rekord törölve."
                        });
                        break;
                    }
                }
            }

            if (auditEntries.Any())
            {
                context.Set<AuditLog>().AddRange(auditEntries);
                _logger.LogInformation("Audit: {Count} bejegyzés létrehozva {User} által.", auditEntries.Count, userName);
            }
        }

        private static int GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            // A legtöbb entitásnál a PK "Id", "PartnerId", "TaskPMId" stb.
            var idProp = entry.CurrentValues.Properties.FirstOrDefault(p => p.Name.EndsWith("Id"));
            if (idProp != null && entry.CurrentValues[idProp] is int intId)
                return intId;

            return 0; // fallback
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            AuditChangesAsync(eventData.Context!).GetAwaiter().GetResult();
            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await AuditChangesAsync(eventData.Context!);
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
