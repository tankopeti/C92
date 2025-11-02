using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Services
{
    public class ResourceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ResourceService> _logger;

        public ResourceService(ApplicationDbContext context, IMapper mapper, ILogger<ResourceService> logger)
        {
            _context = context;
            _mapper = mapper;
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

            var dto = _mapper.Map<ResourceDto>(resource);
            EnrichWithHistory(dto, resource.ResourceHistories);
            return dto;
        }

        // --------------------------------------------------------------
        // GET ALL – SINGLE ROUND-TRIP (projected)
        // --------------------------------------------------------------
        public async Task<IEnumerable<ResourceDto>> GetAllResourcesAsync()
        {
            var dtos = await _context.Resources
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
                .Select(r => new ResourceDto
                {
                    ResourceId = r.ResourceId,
                    Name = r.Name,
                    ResourceTypeId = r.ResourceTypeId,
                    ResourceTypeName = r.ResourceType!.Name,
                    ResourceStatusId = r.ResourceStatusId,
                    ResourceStatusName = r.ResourceStatus!.Name,
                    Serial = r.Serial,
                    NextService = r.NextService,
                    DateOfPurchase = r.DateOfPurchase,
                    WarrantyPeriod = r.WarrantyPeriod,
                    WarrantyExpireDate = r.WarrantyExpireDate,
                    ServiceDate = r.ServiceDate,
                    WhoBuyId = r.WhoBuyId,
                    WhoBuyName = r.WhoBuy != null ? r.WhoBuy.NormalizedUserName : null,
                    WhoLastServicedId = r.WhoLastServicedId,
                    WhoLastServicedName = r.WhoLastServiced != null ? r.WhoLastServiced.NormalizedUserName : null,
                    PartnerId = r.PartnerId,
                    PartnerName = r.Partner!.Name,
                    SiteId = r.SiteId,
                    SiteName = r.Site!.SiteName,
                    ContactId = r.ContactId,
                    ContactName = r.Contact!.FirstName,
                    EmployeeId = r.EmployeeId,
                    EmployeeName = r.Employee!.FirstName,
                    Price = r.Price,
                    CreatedDate = r.CreatedDate,
                    IsActive = r.IsActive,
                    Comment1 = r.Comment1,
                    Comment2 = r.Comment2,

                    // History summary via sub-query
                    HistoryCount = r.ResourceHistories.Count,
                    LastChangeDescription = r.ResourceHistories
                        .OrderByDescending(h => h.ModifiedDate)
                        .Select(h => h.ChangeDescription)
                        .FirstOrDefault(),
                    LastModifiedDate = r.ResourceHistories
                        .OrderByDescending(h => h.ModifiedDate)
                        .Select(h => (DateTime?)h.ModifiedDate)
                        .FirstOrDefault(),
                    LastModifiedByName = r.ResourceHistories
                        .OrderByDescending(h => h.ModifiedDate)
                        .Select(h => h.ModifiedBy!.UserName)
                        .FirstOrDefault() ?? "Unknown",
                    LastServicePrice = r.ResourceHistories
                        .OrderByDescending(h => h.ModifiedDate)
                        .Select(h => h.ServicePrice)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return dtos;
        }

        // --------------------------------------------------------------
        // CREATE
        // --------------------------------------------------------------
        public async Task<ResourceDto> CreateResourceAsync(CreateResourceDto createDto, string? modifiedById = null)
        {
            var resource = _mapper.Map<Resource>(createDto);
            resource.CreatedDate = DateTime.UtcNow;

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            await AddHistoryInternalAsync(resource.ResourceId,
                new ResourceHistoryDto { ChangeDescription = "Resource created.", ServicePrice = null },
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
                .Include(r => r.ResourceHistories)
                .FirstOrDefaultAsync(r => r.ResourceId == updateDto.ResourceId);

            if (resource == null)
            {
                _logger.LogWarning("Update failed: Resource {ResourceId} not found or inactive.", updateDto.ResourceId);
                return null;
            }

            _mapper.Map(updateDto, resource);
            await _context.SaveChangesAsync();

            await AddHistoryInternalAsync(resource.ResourceId,
                new ResourceHistoryDto
                {
                    ChangeDescription = updateDto.Comment1 ?? "Resource updated.",
                    ServicePrice = null
                },
                modifiedById);

            _logger.LogInformation("Resource updated: {ResourceId} by {UserId}", resource.ResourceId, modifiedById ?? "System");
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
            var history = _mapper.Map<ResourceHistory>(dto);
            history.ResourceId = resourceId;
            history.ModifiedById = modifiedById ?? history.ModifiedById;
            history.ModifiedDate = DateTime.UtcNow;

            _context.ResourceHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        // --------------------------------------------------------------
        // PUBLIC: Add manual history entry (e.g., service)
        // --------------------------------------------------------------
        public async Task<ResourceHistoryDto?> AddHistoryAsync(int resourceId,
                                                               ResourceHistoryDto historyDto,
                                                               string? modifiedById = null)
        {
            var exists = await _context.Resources
                .Where(r => r.IsActive == true || r.IsActive == null)
                .AnyAsync(r => r.ResourceId == resourceId);

            if (!exists)
            {
                _logger.LogWarning("History add failed: Resource {ResourceId} not found or inactive.", resourceId);
                return null;
            }

            await AddHistoryInternalAsync(resourceId, historyDto, modifiedById);
            historyDto.ModifiedDate = DateTime.UtcNow;
            return historyDto;
        }

        // --------------------------------------------------------------
        // GET HISTORY for a resource
        // --------------------------------------------------------------
        public async Task<IEnumerable<ResourceHistoryDto>> GetHistoryAsync(int resourceId)
        {
            return await _context.ResourceHistories
                .Where(h => h.ResourceId == resourceId)
                .Include(h => h.ModifiedBy)
                .OrderByDescending(h => h.ModifiedDate)
                .Select(h => new ResourceHistoryDto
                {
                    ResourceHistoryId = h.ResourceHistoryId,
                    ResourceId = h.ResourceId,
                    ModifiedById = h.ModifiedById,
                    ModifiedByName = h.ModifiedBy!.UserName ?? "Unknown",
                    ModifiedDate = h.ModifiedDate,
                    ChangeDescription = h.ChangeDescription,
                    ServicePrice = h.ServicePrice
                })
                .ToListAsync();
        }

        // --------------------------------------------------------------
        // HELPER: Enrich DTO with history summary
        // --------------------------------------------------------------
        // Add/replace in ResourceService.cs
        public async Task<PagedResult<ResourceDto>> GetPagedResourcesAsync(
            int page, int pageSize, string? searchTerm, string? sort, string? order)
        {
            var query = _context.Resources
                .Where(r => r.IsActive == true || r.IsActive == null);

            // FIXED: No null-propagating operator in LINQ-to-Entities
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(r =>
                    (r.Name != null && r.Name.ToLower().Contains(searchLower)) ||
                    (r.Serial != null && r.Serial.ToLower().Contains(searchLower))
                );
            }

            var total = await query.CountAsync();

            // Sorting (unchanged)
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

            // Inline mapping – no profile needed
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
                WhoBuyName = r.WhoBuy?.NormalizedUserName,
                WhoLastServicedId = r.WhoLastServicedId,
                WhoLastServicedName = r.WhoLastServiced?.NormalizedUserName,
                PartnerId = r.PartnerId,
                PartnerName = r.Partner?.Name,
                SiteId = r.SiteId,
                SiteName = r.Site?.SiteName,
                ContactId = r.ContactId,
                ContactName = r.Contact?.FirstName,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee?.FirstName,
                CreatedDate = r.CreatedDate,
                IsActive = r.IsActive,
                Comment1 = r.Comment1,
                Comment2 = r.Comment2,
                CreatedAt = r.CreatedAt,
                // History fields filled below
                HistoryCount = 0,
                LastChangeDescription = null,
                LastModifiedDate = null,
                LastModifiedByName = null,
                LastServicePrice = null
            }).ToList();


            // Fill history summary manually
            foreach (var dto in dtos)
            {
                var resource = items.First(x => x.ResourceId == dto.ResourceId);
                if (resource.ResourceHistories?.Any() == true)
                {
                    var last = resource.ResourceHistories.OrderByDescending(h => h.ModifiedDate).First();
                    dto.HistoryCount = resource.ResourceHistories.Count;
                    dto.LastChangeDescription = last.ChangeDescription;
                    dto.LastModifiedDate = last.ModifiedDate;
                    dto.LastModifiedByName = last.ModifiedBy?.UserName ?? "Unknown";
                    dto.LastServicePrice = last.ServicePrice;
                }
            }

            return new PagedResult<ResourceDto>
            {
                Items = dtos,
                TotalCount = total
            };
        }
        // FIXED: EnrichWithHistory (null-safe)
        private static void EnrichWithHistory(ResourceDto dto, ICollection<ResourceHistory> histories)
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

        // PagedResult class
        public class PagedResult<T>
        {
            public List<T> Items { get; set; } = new();
            public int TotalCount { get; set; }
        }

    }
}