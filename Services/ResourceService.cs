using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Services
{
    public class ResourceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResourceService> _logger;

        public ResourceService(ApplicationDbContext context, ILogger<ResourceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // GET SINGLE
        // --------------------------------------------------------------
        public async Task<ResourceDto?> GetResourceByIdAsync(int resourceId)
        {
            var resource = await _context.Resources
                .Where(r => r.IsActive == true || r.IsActive == null)
                .Include(r => r.ResourceType)
                .Include(r => r.ResourceStatus)
                .Include(r => r.WhoBuy)
                .Include(r => r.WhoLastServiced)
                .Include(r => r.Partner)
                .Include(r => r.Site)
                .Include(r => r.Contact)
                .Include(r => r.Employee)
                .Include(r => r.ResourceHistories)!.ThenInclude(h => h.ModifiedBy)
                .FirstOrDefaultAsync(r => r.ResourceId == resourceId);

            if (resource == null)
            {
                _logger.LogWarning("Resource {ResourceId} not found or inactive.", resourceId);
                return null;
            }

            var dto = new ResourceDto
            {
                ResourceId = resource.ResourceId,
                Name = resource.Name,
                Serial = resource.Serial,
                Price = resource.Price,
                DateOfPurchase = resource.DateOfPurchase,
                ResourceTypeId = resource.ResourceTypeId,
                ResourceTypeName = resource.ResourceType?.Name,
                ResourceStatusId = resource.ResourceStatusId,
                ResourceStatusName = resource.ResourceStatus?.Name,
                NextService = resource.NextService,
                ServiceDate = resource.ServiceDate,
                WarrantyPeriod = resource.WarrantyPeriod,
                WarrantyExpireDate = resource.WarrantyExpireDate,
                WhoBuyId = resource.WhoBuyId,
                WhoBuyName = resource.WhoBuy?.UserName,
                WhoLastServicedId = resource.WhoLastServicedId,
                WhoLastServicedName = resource.WhoLastServiced?.UserName,
                PartnerId = resource.PartnerId,
                PartnerName = resource.Partner?.Name,
                SiteId = resource.SiteId,
                SiteName = resource.Site?.SiteName,
                ContactId = resource.ContactId,
                ContactName = resource.Contact != null ? $"{resource.Contact.FirstName} {resource.Contact.LastName}" : null,
                EmployeeId = resource.EmployeeId,
                EmployeeName = resource.Employee != null ? $"{resource.Employee.FirstName} {resource.Employee.LastName}" : null,
                Comment1 = resource.Comment1,
                Comment2 = resource.Comment2,
                CreatedDate = resource.CreatedDate,
                IsActive = resource.IsActive,
                CreatedAt = resource.CreatedAt
            };

            EnrichWithHistory(dto, resource.ResourceHistories);
            return dto;
        }

        // --------------------------------------------------------------
        // GET ALL – SINGLE ROUND-TRIP (projected)
        // --------------------------------------------------------------
        public async Task<IEnumerable<ResourceDto>> GetAllResourcesAsync()
        {
            var resources = await _context.Resources
                .Where(r => r.IsActive == true || r.IsActive == null)
                .Include(r => r.ResourceType)
                .Include(r => r.ResourceStatus)
                .Include(r => r.WhoBuy)
                .Include(r => r.WhoLastServiced)
                .Include(r => r.Partner)
                .Include(r => r.Site)
                .Include(r => r.Contact)
                .Include(r => r.Employee)
                .Include(r => r.ResourceHistories)!.ThenInclude(h => h.ModifiedBy)
                .ToListAsync();

            var dtos = resources.Select(r => new ResourceDto
            {
                ResourceId = r.ResourceId,
                Name = r.Name,
                ResourceTypeId = r.ResourceTypeId,
                ResourceTypeName = r.ResourceType?.Name,
                ResourceStatusId = r.ResourceStatusId,
                ResourceStatusName = r.ResourceStatus?.Name,
                Serial = r.Serial,
                NextService = r.NextService,
                DateOfPurchase = r.DateOfPurchase,
                WarrantyPeriod = r.WarrantyPeriod,
                WarrantyExpireDate = r.WarrantyExpireDate,
                ServiceDate = r.ServiceDate,
                WhoBuyId = r.WhoBuyId,
                WhoBuyName = r.WhoBuy?.UserName,
                WhoLastServicedId = r.WhoLastServicedId,
                WhoLastServicedName = r.WhoLastServiced?.UserName,
                PartnerId = r.PartnerId,
                PartnerName = r.Partner?.Name,
                SiteId = r.SiteId,
                SiteName = r.Site?.SiteName,
                ContactId = r.ContactId,
                ContactName = r.Contact != null ? $"{r.Contact.FirstName} {r.Contact.LastName}" : null,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee != null ? $"{r.Employee.FirstName} {r.Employee.LastName}" : null,
                Price = r.Price,
                CreatedDate = r.CreatedDate,
                IsActive = r.IsActive,
                Comment1 = r.Comment1,
                Comment2 = r.Comment2,
                CreatedAt = r.CreatedAt
            }).ToList();

            foreach (var dto in dtos)
            {
                var resource = resources.First(x => x.ResourceId == dto.ResourceId);
                EnrichWithHistory(dto, resource.ResourceHistories);
            }

            return dtos;
        }

        // --------------------------------------------------------------
        // CREATE
        // --------------------------------------------------------------
        public async Task<ResourceDto> CreateResourceAsync(CreateResourceDto createDto, string? modifiedById = null)
        {
            var resource = new Resource
            {
                Name = createDto.Name,
                Serial = createDto.Serial,
                Price = createDto.Price,
                DateOfPurchase = createDto.DateOfPurchase,
                ResourceTypeId = createDto.ResourceTypeId,
                ResourceStatusId = createDto.ResourceStatusId,
                NextService = createDto.NextService,
                ServiceDate = createDto.ServiceDate,
                WarrantyPeriod = createDto.WarrantyPeriod,
                WarrantyExpireDate = createDto.WarrantyExpireDate,
                WhoBuyId = createDto.WhoBuyId,
                WhoLastServicedId = createDto.WhoLastServicedId,
                PartnerId = createDto.PartnerId,
                SiteId = createDto.SiteId,
                ContactId = createDto.ContactId,
                EmployeeId = createDto.EmployeeId,
                Comment1 = createDto.Comment1,
                Comment2 = createDto.Comment2,
                CreatedDate = DateTime.UtcNow,
                IsActive = createDto.IsActive,
                CreatedAt = createDto.CreatedAt
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

await AddHistoryInternalAsync(resource.ResourceId,
    new ResourceHistoryDto
    {
        ChangeDescription = "Resource created.",
        ServicePrice = null
    },
    modifiedById);

            _logger.LogInformation("Resource created: {ResourceId} by {UserId}", resource.ResourceId, modifiedById ?? "System");
            return await GetResourceByIdAsync(resource.ResourceId)!;
        }

        // --------------------------------------------------------------
        // UPDATE
        // --------------------------------------------------------------
public async Task<ResourceDto?> UpdateResourceAsync(UpdateResourceDto updateDto, string? modifiedById = null)
{
    var resource = await _context.Resources
        .Where(r => r.IsActive == true || r.IsActive == null)
        .Include(r => r.ResourceType)
        .Include(r => r.ResourceStatus)
        .Include(r => r.WhoBuy)
        .Include(r => r.WhoLastServiced)
        .Include(r => r.Partner)
        .Include(r => r.Site)
        .Include(r => r.Contact)
        .Include(r => r.Employee)
        .Include(r => r.ResourceHistories)
        .FirstOrDefaultAsync(r => r.ResourceId == updateDto.ResourceId);

    if (resource == null)
    {
        _logger.LogWarning("Update failed: Resource {ResourceId} not found or inactive.", updateDto.ResourceId);
        return null;
    }

    var changes = new List<string>();

    // --- LOOKUP TÁBLÁK (NINCS IsActive!) ---
    var typeNames = await _context.ResourceTypes
        .ToDictionaryAsync(t => t.ResourceTypeId, t => t.Name ?? "Ismeretlen");

    var statusNames = await _context.ResourceStatuses
        .ToDictionaryAsync(s => s.ResourceStatusId, s => s.Name ?? "Ismeretlen");

    var userNames = await _context.Users
        .ToDictionaryAsync(u => u.Id, u => u.UserName ?? "Rendszer");

    var partnerNames = await _context.Partners
        .Select(p => new { p.PartnerId, p.Name })
        .ToDictionaryAsync(p => p.PartnerId, p => p.Name ?? "Ismeretlen");

    var siteNames = await _context.Sites
        .Select(s => new { s.SiteId, s.SiteName })
        .ToDictionaryAsync(s => s.SiteId, s => s.SiteName ?? "Ismeretlen");

    var contactNames = await _context.Contacts
        .Select(c => new { c.ContactId, FullName = $"{c.FirstName} {c.LastName}".Trim() })
        .ToDictionaryAsync(c => c.ContactId, c => c.FullName);

    var employeeNames = await _context.Employees
        .Select(e => new { e.EmployeeId, FullName = $"{e.FirstName} {e.LastName}".Trim() })
        .ToDictionaryAsync(e => e.EmployeeId, e => e.FullName);

    // --- SEGÉDFÜGGVÉNY ---
    void AddChange(string field, object? oldVal, object? newVal, Func<object?, string> formatter)
    {
        if (Equals(oldVal, newVal)) return;
        changes.Add($"{field}: {formatter(oldVal)} → {formatter(newVal)}");
    }

    // --- FORMATTING ---
    string FormatType(object? val) => val is int id ? typeNames.GetValueOrDefault(id, "—") : "—";
    string FormatStatus(object? val) => val is int id ? statusNames.GetValueOrDefault(id, "—") : "—";
    string FormatUser(object? val) => val is string id ? userNames.GetValueOrDefault(id, "—") : "—";
    string FormatPartner(object? val) => val is int id ? partnerNames.GetValueOrDefault(id, "—") : "—";
    string FormatSite(object? val) => val is int id ? siteNames.GetValueOrDefault(id, "—") : "—";
    string FormatContact(object? val) => val is int id ? contactNames.GetValueOrDefault(id, "—") : "—";
    string FormatEmployee(object? val) => val is int id ? employeeNames.GetValueOrDefault(id, "—") : "—";

    // --- VÁLTOZÁSOK ---
    AddChange("Név", resource.Name, updateDto.Name, v => v?.ToString() ?? "—");
    AddChange("Sorozatszám", resource.Serial, updateDto.Serial, v => v?.ToString() ?? "—");
    AddChange("Ár", resource.Price, updateDto.Price, v => v is decimal d ? d.ToString("N0") : "—");
    AddChange("Vétel dátuma", resource.DateOfPurchase, updateDto.DateOfPurchase, v => v is DateTime dt ? dt.ToString("yyyy-MM-dd") : "—");
    AddChange("Típus", resource.ResourceTypeId, updateDto.ResourceTypeId, FormatType);
    AddChange("Státusz", resource.ResourceStatusId, updateDto.ResourceStatusId, FormatStatus);
    AddChange("Következő szerviz", resource.NextService, updateDto.NextService, v => v is DateTime dt ? dt.ToString("yyyy-MM-dd") : "—");
    AddChange("Szerviz dátuma", resource.ServiceDate, updateDto.ServiceDate, v => v is DateTime dt ? dt.ToString("yyyy-MM-dd") : "—");
    AddChange("Garancia (hónap)", resource.WarrantyPeriod, updateDto.WarrantyPeriod, v => v?.ToString() ?? "—");
    AddChange("Garancia lejárat", resource.WarrantyExpireDate, updateDto.WarrantyExpireDate, v => v is DateTime dt ? dt.ToString("yyyy-MM-dd") : "—");
    AddChange("Vásárló", resource.WhoBuyId, updateDto.WhoBuyId, FormatUser);
    AddChange("Utolsó szervizelő", resource.WhoLastServicedId, updateDto.WhoLastServicedId, FormatUser);
    AddChange("Partner", resource.PartnerId, updateDto.PartnerId, FormatPartner);
    AddChange("Telephely", resource.SiteId, updateDto.SiteId, FormatSite);
    AddChange("Kapcsolattartó", resource.ContactId, updateDto.ContactId, FormatContact);
    AddChange("Munkatárs", resource.EmployeeId, updateDto.EmployeeId, FormatEmployee);

    // Comment1 / Comment2
    if (!string.IsNullOrWhiteSpace(updateDto.Comment1) && resource.Comment1 != updateDto.Comment1)
        changes.Add($"Megjegyzés 1: {updateDto.Comment1.Trim()}");
    if (!string.IsNullOrWhiteSpace(updateDto.Comment2) && resource.Comment2 != updateDto.Comment2)
        changes.Add($"Megjegyzés 2: {updateDto.Comment2.Trim()}");

    var changeDescription = changes.Any()
        ? string.Join("; ", changes)
        : "Resource updated.";

    // --- ENTITÁS FRISSÍTÉSE ---
    resource.Name = updateDto.Name;
    resource.Serial = updateDto.Serial;
    resource.Price = updateDto.Price;
    resource.DateOfPurchase = updateDto.DateOfPurchase;
    resource.ResourceTypeId = updateDto.ResourceTypeId;
    resource.ResourceStatusId = updateDto.ResourceStatusId;
    resource.NextService = updateDto.NextService;
    resource.ServiceDate = updateDto.ServiceDate;
    resource.WarrantyPeriod = updateDto.WarrantyPeriod;
    resource.WarrantyExpireDate = updateDto.WarrantyExpireDate;
    resource.WhoBuyId = updateDto.WhoBuyId;
    resource.WhoLastServicedId = updateDto.WhoLastServicedId;
    resource.PartnerId = updateDto.PartnerId;
    resource.SiteId = updateDto.SiteId;
    resource.ContactId = updateDto.ContactId;
    resource.EmployeeId = updateDto.EmployeeId;
    resource.Comment1 = updateDto.Comment1;
    resource.Comment2 = updateDto.Comment2;

    await _context.SaveChangesAsync();

    // --- HISTORY ---
    await AddHistoryInternalAsync(resource.ResourceId,
        new ResourceHistoryDto
        {
            ChangeDescription = changeDescription,
            ServicePrice = null
        },
        modifiedById);

    _logger.LogInformation("Resource updated: {ResourceId} | {Changes}", resource.ResourceId, changeDescription);

    return await GetResourceByIdAsync(resource.ResourceId);
}


        // --------------------------------------------------------------
        // DEACTIVATE (soft delete)
        // --------------------------------------------------------------
        public async Task<bool> DeactivateResourceAsync(int resourceId, string? modifiedById = null)
        {
            var resource = await _context.Resources.FindAsync(resourceId);
            if (resource == null || resource.IsActive == false)
            {
                _logger.LogWarning("Deactivate failed: Resource {ResourceId} not found or already inactive.", resourceId);
                return false;
            }

            resource.IsActive = false;
            await _context.SaveChangesAsync();

            await AddHistoryInternalAsync(resourceId,
                new ResourceHistoryDto { ChangeDescription = "Resource deactivated.", ServicePrice = null },
                modifiedById);

            _logger.LogInformation("Resource deactivated: {ResourceId} by {UserId}", resourceId, modifiedById ?? "System");
            return true;
        }

        // --------------------------------------------------------------
        // PRIVATE: Add history (internal helper)
        // --------------------------------------------------------------
private async Task AddHistoryInternalAsync(int resourceId, ResourceHistoryDto dto, string? modifiedById)
{
    var history = new ResourceHistory
    {
        ResourceId = resourceId,
        ModifiedById = modifiedById ?? dto.ModifiedById,
        ModifiedDate = DateTime.UtcNow,
        ChangeDescription = dto.ChangeDescription,
        ServicePrice = dto.ServicePrice
    };

    _context.ResourceHistories.Add(history);
    await _context.SaveChangesAsync();
}

        // --------------------------------------------------------------
        // PUBLIC: Add manual history entry (e.g., service)
        // --------------------------------------------------------------
public async Task<ResourceHistoryDto> AddHistoryAsync(int resourceId, ResourceHistoryDto dto, string? modifiedById = null)
{
    var user = modifiedById != null 
        ? await _context.Users.FindAsync(modifiedById) 
        : null;

    var entity = new ResourceHistory
    {
        ResourceId = resourceId,
        ModifiedById = modifiedById,
        ModifiedDate = DateTime.UtcNow, // ALWAYS set
        ChangeDescription = dto.ChangeDescription?.Trim(),
        ServicePrice = dto.ServicePrice
    };

    _context.ResourceHistories.Add(entity);
    await _context.SaveChangesAsync();

    return new ResourceHistoryDto
    {
        ResourceHistoryId = entity.ResourceHistoryId,
        ResourceId = entity.ResourceId,
        ModifiedById = entity.ModifiedById,
        ModifiedByName = user?.UserName ?? "Rendszer",
        ModifiedDate = entity.ModifiedDate,
        ChangeDescription = entity.ChangeDescription,
        ServicePrice = entity.ServicePrice
    };
}

        // --------------------------------------------------------------
        // GET HISTORY for a resource
        // --------------------------------------------------------------
public async Task<IEnumerable<ResourceHistoryDto>> GetHistoryAsync(int resourceId)
{
    return await _context.ResourceHistories
        .Where(h => h.ResourceId == resourceId)
        .Include(h => h.ModifiedBy)
        .OrderByDescending(h => h.ModifiedDate ?? DateTime.MinValue)
        .Select(h => new ResourceHistoryDto
        {
            ResourceHistoryId = h.ResourceHistoryId,
            ResourceId = h.ResourceId,
            ModifiedById = h.ModifiedById,
            ModifiedByName = h.ModifiedBy != null 
                ? h.ModifiedBy.UserName 
                : "Rendszer",
            ModifiedDate = h.ModifiedDate ?? DateTime.UtcNow, // NEVER null
            ChangeDescription = string.IsNullOrWhiteSpace(h.ChangeDescription) 
                ? "Módosítás" 
                : h.ChangeDescription.Trim(),
            ServicePrice = h.ServicePrice
        })
        .ToListAsync();
}

        // --------------------------------------------------------------
        // PAGED RESOURCES
        // --------------------------------------------------------------
        public async Task<PagedResult<ResourceDto>> GetPagedResourcesAsync(
            int page, int pageSize, string? searchTerm, string? sort, string? order)
        {
            var query = _context.Resources
                .Where(r => r.IsActive == true || r.IsActive == null);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(r =>
                    (r.Name != null && r.Name.ToLower().Contains(searchLower)) ||
                    (r.Serial != null && r.Serial.ToLower().Contains(searchLower))
                );
            }

            var total = await query.CountAsync();

            IOrderedQueryable<Resource> orderedQuery;
            if (sort?.ToLower() == "name")
                orderedQuery = order?.ToLower() == "asc" ? query.OrderBy(r => r.Name) : query.OrderByDescending(r => r.Name);
            else if (sort?.ToLower() == "price")
                orderedQuery = order?.ToLower() == "asc" ? query.OrderBy(r => r.Price) : query.OrderByDescending(r => r.Price);
            else
                orderedQuery = order?.ToLower() == "asc" ? query.OrderBy(r => r.CreatedDate) : query.OrderByDescending(r => r.CreatedDate);

            var items = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(r => r.ResourceType)
                .Include(r => r.ResourceStatus)
                .Include(r => r.WhoBuy)
                .Include(r => r.WhoLastServiced)
                .Include(r => r.ResourceHistories).ThenInclude(h => h.ModifiedBy)
                .ToListAsync();

            var dtos = items.Select(r => new ResourceDto
            {
                ResourceId = r.ResourceId,
                Name = r.Name,
                Serial = r.Serial,
                ResourceTypeId = r.ResourceTypeId,
                ResourceTypeName = r.ResourceType?.Name,
                ResourceStatusId = r.ResourceStatusId,
                ResourceStatusName = r.ResourceStatus?.Name,
                Price = r.Price,
                DateOfPurchase = r.DateOfPurchase,
                WarrantyPeriod = r.WarrantyPeriod,
                WarrantyExpireDate = r.WarrantyExpireDate,
                ServiceDate = r.ServiceDate,
                WhoBuyId = r.WhoBuyId,
                WhoBuyName = r.WhoBuy?.UserName,
                WhoLastServicedId = r.WhoLastServicedId,
                WhoLastServicedName = r.WhoLastServiced?.UserName,
                PartnerId = r.PartnerId,
                PartnerName = r.Partner?.Name,
                SiteId = r.SiteId,
                SiteName = r.Site?.SiteName,
                ContactId = r.ContactId,
                ContactName = r.Contact != null ? $"{r.Contact.FirstName} {r.Contact.LastName}" : null,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee != null ? $"{r.Employee.FirstName} {r.Employee.LastName}" : null,
                CreatedDate = r.CreatedDate,
                IsActive = r.IsActive,
                Comment1 = r.Comment1,
                Comment2 = r.Comment2,
                CreatedAt = r.CreatedAt
            }).ToList();

            foreach (var dto in dtos)
            {
                var resource = items.First(x => x.ResourceId == dto.ResourceId);
                EnrichWithHistory(dto, resource.ResourceHistories);
            }

            return new PagedResult<ResourceDto>
            {
                Items = dtos,
                TotalCount = total
            };
        }

        // --------------------------------------------------------------
        // HELPER: Enrich DTO with history summary
        // --------------------------------------------------------------
        private static void EnrichWithHistory(ResourceDto dto, ICollection<ResourceHistory>? histories)
        {
            if (histories == null || !histories.Any()) return;

            var last = histories.OrderByDescending(h => h.ModifiedDate).FirstOrDefault();
            if (last == null) return;

            dto.HistoryCount = histories.Count;
            dto.LastChangeDescription = last.ChangeDescription;
            dto.LastModifiedDate = last.ModifiedDate;
            dto.LastModifiedByName = last.ModifiedBy?.UserName ?? "Unknown";
            dto.LastServicePrice = last.ServicePrice;
        }

        // --------------------------------------------------------------
        // PagedResult
        // --------------------------------------------------------------
        public class PagedResult<T>
        {
            public List<T> Items { get; set; } = new();
            public int TotalCount { get; set; }
        }
    }
}